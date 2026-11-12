using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using SafeKeyBinder.Models;
using SafeKeyBinder.Native;

namespace SafeKeyBinder.Services
{
    public class KeyboardHookService : IDisposable
    {
        private readonly WindowService _windowService;
        private readonly InputSenderService _inputSender;
        private readonly Win32Native.LowLevelKeyboardProc _hookProcDelegate;

        private readonly object _stateLock = new object();
        private readonly object _queueLock = new object();
        private readonly Queue<KeybindItem> _sendQueue = new Queue<KeybindItem>();
        private readonly AutoResetEvent _sendSignal = new AutoResetEvent(false);

        private IntPtr _hookHandle = IntPtr.Zero;
        private Thread _workerThread;
        private volatile bool _workerRunning = false;
        private volatile bool _captureSuspend = false;

        private AppConfig _config;
        private BinderStatus _status = BinderStatus.Stopped;
        private bool _manualEnabled = false;
        private bool _autoSequenceHookEnabled = false;
        private int _activeSuppressedTriggerVk = 0;

        private bool _physLCtrl = false;
        private bool _physRCtrl = false;
        private bool _physLAlt = false;
        private bool _physRAlt = false;
        private bool _physLShift = false;
        private bool _physRShift = false;

        public event Action<BinderStatus> StatusChanged;
        public event Action<KeybindItem> KeybindTriggered;
        public event Action AutoSequencePauseToggleRequested;
        public event Action AutoSequenceStopRequested;

        public KeyboardHookService(WindowService windowService, InputSenderService inputSender)
        {
            _windowService = windowService;
            _inputSender = inputSender;
            _hookProcDelegate = HookCallback;
            _config = new AppConfig();
        }

        public BinderStatus Status
        {
            get
            {
                lock (_stateLock)
                {
                    return _status;
                }
            }
        }

        public bool IsHookInstalled
        {
            get { return _hookHandle != IntPtr.Zero; }
        }

        public bool IsManualActive
        {
            get
            {
                lock (_stateLock)
                {
                    return _manualEnabled;
                }
            }
        }

        public void SetConfig(AppConfig config)
        {
            lock (_stateLock)
            {
                _config = config ?? new AppConfig();
            }
        }

        public void SetCaptureSuspend(bool suspend)
        {
            _captureSuspend = suspend;
            if (suspend)
            {
                ResetModifierStates();
                _activeSuppressedTriggerVk = 0;
            }
        }

        public void Start(AppConfig config)
        {
            lock (_stateLock)
            {
                _config = config ?? new AppConfig();
                _manualEnabled = true;
            }

            EnsureHookInstalled();
            ResetModifierStates();
            _activeSuppressedTriggerVk = 0;
            UpdateStatus(BinderStatus.Active);
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                _manualEnabled = false;
            }

            UpdateStatus(BinderStatus.Stopped);
            UninstallHookIfUnused();
        }

        public void EnableAutoSequenceHook(AppConfig config)
        {
            lock (_stateLock)
            {
                if (config != null) _config = config;
                _autoSequenceHookEnabled = true;
            }
            EnsureHookInstalled();
        }

        public void DisableAutoSequenceHook()
        {
            lock (_stateLock)
            {
                _autoSequenceHookEnabled = false;
            }
            UninstallHookIfUnused();
        }

        public void ShutdownAll()
        {
            lock (_stateLock)
            {
                _manualEnabled = false;
                _autoSequenceHookEnabled = false;
            }
            UninstallHookForced();
            UpdateStatus(BinderStatus.Stopped);
        }

        private void EnsureHookInstalled()
        {
            if (_hookHandle != IntPtr.Zero) return;

            StartWorkerThread();

            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr hMod = Win32Native.GetModuleHandle(curModule.ModuleName);
                _hookHandle = Win32Native.SetWindowsHookEx(
                    Win32Native.WH_KEYBOARD_LL,
                    _hookProcDelegate,
                    hMod,
                    0);
            }

            if (_hookHandle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                StopWorkerThread();
                throw new Win32Exception(err, "Gagal memasang global keyboard hook Windows.");
            }
        }

        private void UninstallHookIfUnused()
        {
            bool keepHook;
            lock (_stateLock)
            {
                keepHook = _manualEnabled || _autoSequenceHookEnabled;
            }
            if (!keepHook)
            {
                UninstallHookForced();
            }
        }

        private void UninstallHookForced()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                Win32Native.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            StopWorkerThread();
            ResetModifierStates();
            _activeSuppressedTriggerVk = 0;
        }

        private void StartWorkerThread()
        {
            if (_workerRunning) return;

            _workerRunning = true;
            _workerThread = new Thread(WorkerLoop);
            _workerThread.IsBackground = true;
            _workerThread.Name = "SafeKeyBinder_InputWorker";
            _workerThread.Start();
        }

        private void StopWorkerThread()
        {
            _workerRunning = false;
            _sendSignal.Set();

            lock (_queueLock)
            {
                _sendQueue.Clear();
            }
        }

        private void WorkerLoop()
        {
            while (_workerRunning)
            {
                _sendSignal.WaitOne();
                if (!_workerRunning) break;

                while (_workerRunning)
                {
                    KeybindItem nextItem = null;
                    lock (_queueLock)
                    {
                        if (_sendQueue.Count > 0)
                        {
                            nextItem = _sendQueue.Dequeue();
                        }
                    }

                    if (nextItem == null) break;

                    int delayMs = 10;
                    int prefixDelayMs = 80;
                    lock (_stateLock)
                    {
                        if (_config != null)
                        {
                            delayMs = _config.TypingDelayMs;
                            prefixDelayMs = _config.ManualPrefixDelayMs;
                        }
                    }

                    try
                    {
                        _inputSender.SendKeybind(nextItem, delayMs, prefixDelayMs);
                        Action<KeybindItem> handler = KeybindTriggered;
                        if (handler != null)
                        {
                            handler(nextItem);
                        }
                    }
                    catch
                    {
                        // Keep worker alive even if target window closes mid-send
                    }
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && !_captureSuspend)
            {
                Win32Native.KBDLLHOOKSTRUCT kbd =
                    (Win32Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32Native.KBDLLHOOKSTRUCT));

                bool isInjected = (kbd.flags & Win32Native.LLKHF_INJECTED) != 0 ||
                                  (kbd.flags & Win32Native.LLKHF_LOWER_IL_INJECTED) != 0;

                if (!isInjected)
                {
                    int msg = wParam.ToInt32();
                    bool isKeyDown = (msg == Win32Native.WM_KEYDOWN || msg == Win32Native.WM_SYSKEYDOWN);
                    bool isKeyUp = (msg == Win32Native.WM_KEYUP || msg == Win32Native.WM_SYSKEYUP);
                    int vk = (int)kbd.vkCode;

                    if (UpdatePhysicalModifier(vk, isKeyDown, isKeyUp))
                    {
                        return Win32Native.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                    }

                    if (isKeyUp)
                    {
                        if (vk == _activeSuppressedTriggerVk)
                        {
                            _activeSuppressedTriggerVk = 0;
                            return (IntPtr)1;
                        }
                    }
                    else if (isKeyDown)
                    {
                        if (vk == _activeSuppressedTriggerVk)
                        {
                            return (IntPtr)1;
                        }

                        bool ctrlDown = IsCtrlDown();
                        bool altDown = IsAltDown();
                        bool shiftDown = IsShiftDown();

                        AppConfig snapshotConfig;
                        BinderStatus snapshotStatus;
                        bool autoSeqActive;
                        bool manualActive;
                        lock (_stateLock)
                        {
                            snapshotConfig = _config;
                            snapshotStatus = _status;
                            autoSeqActive = _autoSequenceHookEnabled;
                            manualActive = _manualEnabled;
                        }

                        // 1. Auto-Sequence Emergency Stop & Pause/Resume Hotkeys take priority while running
                        if (autoSeqActive && snapshotConfig != null && snapshotConfig.AutoSequence != null)
                        {
                            AutoSequenceConfig seq = snapshotConfig.AutoSequence;

                            if (seq.StopVirtualKey > 0 &&
                                vk == seq.StopVirtualKey &&
                                ctrlDown == seq.StopCtrl &&
                                altDown == seq.StopAlt &&
                                shiftDown == seq.StopShift)
                            {
                                _activeSuppressedTriggerVk = vk;
                                Action stopHandler = AutoSequenceStopRequested;
                                if (stopHandler != null) stopHandler();
                                return (IntPtr)1;
                            }

                            if (seq.PauseResumeVirtualKey > 0 &&
                                vk == seq.PauseResumeVirtualKey &&
                                ctrlDown == seq.PauseResumeCtrl &&
                                altDown == seq.PauseResumeAlt &&
                                shiftDown == seq.PauseResumeShift)
                            {
                                _activeSuppressedTriggerVk = vk;
                                Action pauseHandler = AutoSequencePauseToggleRequested;
                                if (pauseHandler != null) pauseHandler();
                                return (IntPtr)1;
                            }
                        }

                        if (!manualActive || snapshotStatus == BinderStatus.Stopped)
                        {
                            return Win32Native.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                        }

                        // 2. Manual Keybinds
                        WindowInfo activeWindow = null;
                        if (snapshotConfig != null &&
                            snapshotConfig.TargetScope == TargetScopeMode.WindowFilter)
                        {
                            activeWindow = _windowService.GetForegroundWindowInfo();
                        }

                        HookMatchResult result = KeybindMatcher.EvaluateKeyDown(
                            snapshotConfig,
                            snapshotStatus,
                            vk,
                            ctrlDown,
                            altDown,
                            shiftDown,
                            activeWindow);

                        if (result.Action == HookMatchAction.ToggleMaster)
                        {
                            _activeSuppressedTriggerVk = vk;
                            BinderStatus nextStatus = (snapshotStatus == BinderStatus.Active)
                                ? BinderStatus.PausedByMaster
                                : BinderStatus.Active;
                            UpdateStatus(nextStatus);
                            return (IntPtr)1;
                        }

                        if (result.Action == HookMatchAction.TypingPause)
                        {
                            UpdateStatus(BinderStatus.PausedByTyping);
                            return Win32Native.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                        }

                        if (result.Action == HookMatchAction.TypingResume)
                        {
                            UpdateStatus(BinderStatus.Active);
                            return Win32Native.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                        }

                        if (result.Action == HookMatchAction.TriggerKeybind && result.MatchedItem != null)
                        {
                            _activeSuppressedTriggerVk = vk;
                            EnqueueSend(result.MatchedItem.Clone());
                            return (IntPtr)1;
                        }
                    }
                }
            }

            return Win32Native.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private void EnqueueSend(KeybindItem item)
        {
            lock (_queueLock)
            {
                _sendQueue.Enqueue(item);
            }
            _sendSignal.Set();
        }

        private void UpdateStatus(BinderStatus newStatus)
        {
            bool changed = false;
            lock (_stateLock)
            {
                if (_status != newStatus)
                {
                    _status = newStatus;
                    changed = true;
                }
            }

            if (changed)
            {
                Action<BinderStatus> handler = StatusChanged;
                if (handler != null)
                {
                    handler(newStatus);
                }
            }
        }

        private bool UpdatePhysicalModifier(int vk, bool isKeyDown, bool isKeyUp)
        {
            if (!isKeyDown && !isKeyUp) return false;

            switch (vk)
            {
                case Win32Native.VK_LCONTROL:
                    _physLCtrl = isKeyDown;
                    return true;
                case Win32Native.VK_RCONTROL:
                    _physRCtrl = isKeyDown;
                    return true;
                case Win32Native.VK_CONTROL:
                    _physLCtrl = isKeyDown;
                    return true;
                case Win32Native.VK_LMENU:
                    _physLAlt = isKeyDown;
                    return true;
                case Win32Native.VK_RMENU:
                    _physRAlt = isKeyDown;
                    return true;
                case Win32Native.VK_MENU:
                    _physLAlt = isKeyDown;
                    return true;
                case Win32Native.VK_LSHIFT:
                    _physLShift = isKeyDown;
                    return true;
                case Win32Native.VK_RSHIFT:
                    _physRShift = isKeyDown;
                    return true;
                case Win32Native.VK_SHIFT:
                    _physLShift = isKeyDown;
                    return true;
                default:
                    return false;
            }
        }

        private bool IsCtrlDown()
        {
            SyncStuckModifiersIfSafe();
            return _physLCtrl || _physRCtrl || IsAsyncDown(Win32Native.VK_CONTROL);
        }

        private bool IsAltDown()
        {
            SyncStuckModifiersIfSafe();
            return _physLAlt || _physRAlt || IsAsyncDown(Win32Native.VK_MENU);
        }

        private bool IsShiftDown()
        {
            SyncStuckModifiersIfSafe();
            return _physLShift || _physRShift || IsAsyncDown(Win32Native.VK_SHIFT);
        }

        private void SyncStuckModifiersIfSafe()
        {
            double msSinceRelease = (DateTime.UtcNow - _inputSender.LastModifierReleaseUtc).TotalMilliseconds;
            if (msSinceRelease > 700)
            {
                if (!IsAsyncDown(Win32Native.VK_LCONTROL)) _physLCtrl = false;
                if (!IsAsyncDown(Win32Native.VK_RCONTROL)) _physRCtrl = false;
                if (!IsAsyncDown(Win32Native.VK_LMENU)) _physLAlt = false;
                if (!IsAsyncDown(Win32Native.VK_RMENU)) _physRAlt = false;
                if (!IsAsyncDown(Win32Native.VK_LSHIFT)) _physLShift = false;
                if (!IsAsyncDown(Win32Native.VK_RSHIFT)) _physRShift = false;
            }
        }

        private static bool IsAsyncDown(int vk)
        {
            return (Win32Native.GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private void ResetModifierStates()
        {
            _physLCtrl = false;
            _physRCtrl = false;
            _physLAlt = false;
            _physRAlt = false;
            _physLShift = false;
            _physRShift = false;
        }

        public void Dispose()
        {
            ShutdownAll();
            _sendSignal.Close();
        }
    }
}
