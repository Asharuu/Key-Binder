using System;
using System.Collections.Generic;
using System.Threading;
using SafeKeyBinder.Models;

namespace SafeKeyBinder.Services
{
    public class AutoSequenceProgressEventArgs : EventArgs
    {
        public AutoSequenceState State { get; set; }
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public string StatusMessage { get; set; }
        public string LastSentItem { get; set; }
    }

    public class AutoSequenceService : IDisposable
    {
        private readonly WindowService _windowService;
        private readonly InputSenderService _inputSender;
        private readonly KeyboardHookService _hookService;

        private readonly object _lock = new object();
        private Thread _workerThread;
        private volatile bool _stopRequested = false;
        private volatile bool _pauseRequested = false;
        private volatile bool _resumeCountdownPending = false;

        private AutoSequenceState _state = AutoSequenceState.Idle;
        private List<string> _items = new List<string>();
        private int _currentIndex = 0;
        private AutoSequenceConfig _seqConfig = new AutoSequenceConfig();

        public event Action<AutoSequenceProgressEventArgs> ProgressChanged;

        public AutoSequenceService(
            WindowService windowService,
            InputSenderService inputSender,
            KeyboardHookService hookService)
        {
            _windowService = windowService;
            _inputSender = inputSender;
            _hookService = hookService;

            _hookService.AutoSequencePauseToggleRequested += OnHookPauseToggleRequested;
            _hookService.AutoSequenceStopRequested += OnHookStopRequested;
        }

        public AutoSequenceState State
        {
            get
            {
                lock (_lock)
                {
                    return _state;
                }
            }
        }

        public bool IsRunningOrPaused
        {
            get
            {
                AutoSequenceState s = State;
                return s == AutoSequenceState.Countdown ||
                       s == AutoSequenceState.Running ||
                       s == AutoSequenceState.Paused ||
                       s == AutoSequenceState.WaitingWindow;
            }
        }

        public void Start(List<string> items, int startIndex, AppConfig fullConfig)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException("Daftar urutan masih kosong.");
            }

            Stop();

            lock (_lock)
            {
                _items = new List<string>(items);
                _currentIndex = (startIndex >= 0 && startIndex < _items.Count) ? startIndex : 0;
                _seqConfig = CloneConfig(fullConfig != null ? fullConfig.AutoSequence : null);
                _stopRequested = false;
                _pauseRequested = false;
            }

            _hookService.EnableAutoSequenceHook(fullConfig);

            _workerThread = new Thread(RunnerLoop);
            _workerThread.IsBackground = true;
            _workerThread.Name = "SafeKeyBinder_AutoSequence";
            _workerThread.Start();
        }

        public void TogglePause()
        {
            AutoSequenceState current = State;
            if (current == AutoSequenceState.Paused)
            {
                Resume(false);
            }
            else if (current == AutoSequenceState.Countdown ||
                     current == AutoSequenceState.Running ||
                     current == AutoSequenceState.WaitingWindow)
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (!IsRunningOrPaused) return;
            _resumeCountdownPending = false;
            _pauseRequested = true;
        }

        public void Resume()
        {
            Resume(false);
        }

        public void Resume(bool withCountdown)
        {
            if (!IsRunningOrPaused) return;
            _resumeCountdownPending = withCountdown;
            _pauseRequested = false;
        }

        public void SetNextIndex(int nextIndex)
        {
            int count;
            int clamped;
            string lastSentSnapshot = string.Empty;
            lock (_lock)
            {
                count = _items.Count;
                if (count == 0) return;
                clamped = (nextIndex >= 0 && nextIndex < count) ? nextIndex : 0;
                _currentIndex = clamped;
                if (_state != AutoSequenceState.Paused) return;
            }

            string nextItem = _items[clamped];
            PublishProgress(
                AutoSequenceState.Paused,
                clamped,
                count,
                string.Format("DIJEDA (PAUSE) — Berikutnya: {0} ({1}/{2})", nextItem, clamped + 1, count),
                lastSentSnapshot);
        }

        public void UpdateItemsWhilePaused(List<string> items, int nextIndex)
        {
            if (items == null || items.Count == 0) return;
            int clamped;
            lock (_lock)
            {
                if (_state != AutoSequenceState.Paused) return;
                _items = new List<string>(items);
                clamped = (nextIndex >= 0 && nextIndex < _items.Count) ? nextIndex : 0;
                _currentIndex = clamped;
            }

            string nextItem = items[clamped];
            PublishProgress(
                AutoSequenceState.Paused,
                clamped,
                items.Count,
                string.Format("DIJEDA (PAUSE) — Berikutnya: {0} ({1}/{2})", nextItem, clamped + 1, items.Count),
                string.Empty);
        }

        public void Stop()
        {
            _stopRequested = true;
            _pauseRequested = false;

            Thread t = _workerThread;
            if (t != null && t.IsAlive && Thread.CurrentThread != t)
            {
                t.Join(1200);
            }
            _workerThread = null;
        }

        private void OnHookPauseToggleRequested()
        {
            TogglePause();
        }

        private void OnHookStopRequested()
        {
            _stopRequested = true;
            _pauseRequested = false;
        }

        private void RunnerLoop()
        {
            string lastSent = string.Empty;
            bool completedNaturally = false;

            try
            {
                // 1. Optional Startup Countdown
                int countdown = _seqConfig.CountdownSeconds;
                for (int sec = countdown; sec > 0 && !_stopRequested; sec--)
                {
                    if (_pauseRequested)
                    {
                        WaitWhilePaused(lastSent);
                        if (_stopRequested) break;
                    }

                    PublishProgress(
                        AutoSequenceState.Countdown,
                        _currentIndex,
                        _items.Count,
                        string.Format("Mulai dalam {0} detik... (arahkan ke jendela target)", sec),
                        lastSent);

                    if (!SleepInterruptible(1000))
                    {
                        break;
                    }
                }

                // 2. Main Execution Loop
                while (!_stopRequested)
                {
                    if (_pauseRequested)
                    {
                        WaitWhilePaused(lastSent);
                        if (_stopRequested) break;
                    }

                    if (_currentIndex >= _items.Count)
                    {
                        if (_seqConfig.LoopForever && _items.Count > 0)
                        {
                            _currentIndex = 0;
                        }
                        else
                        {
                            completedNaturally = true;
                            break;
                        }
                    }

                    // Optional Target Window Safety Guard
                    if (_seqConfig.RequireTargetWindowFocus &&
                        !string.IsNullOrEmpty((_seqConfig.TargetWindowKeyword ?? string.Empty).Trim()))
                    {
                        bool warnedWaiting = false;
                        while (!_stopRequested && !_pauseRequested && !IsTargetWindowFocused())
                        {
                            if (!warnedWaiting)
                            {
                                PublishProgress(
                                    AutoSequenceState.WaitingWindow,
                                    _currentIndex,
                                    _items.Count,
                                    "Menunggu jendela '" + _seqConfig.TargetWindowKeyword + "' fokus...",
                                    lastSent);
                                warnedWaiting = true;
                            }
                            Thread.Sleep(200);
                        }

                        if (_stopRequested) break;
                        if (_pauseRequested) continue;
                    }

                    string currentItem;
                    int totalCount;
                    int activeIndex;
                    lock (_lock)
                    {
                        if (_currentIndex >= _items.Count)
                        {
                            if (_seqConfig.LoopForever && _items.Count > 0)
                            {
                                _currentIndex = 0;
                            }
                            else
                            {
                                completedNaturally = true;
                                break;
                            }
                        }
                        activeIndex = _currentIndex;
                        totalCount = _items.Count;
                        currentItem = _items[activeIndex];
                    }

                    string textToSend = SequenceGenerator.FormatCommand(_seqConfig.CommandTemplate, currentItem);

                    PublishProgress(
                        AutoSequenceState.Running,
                        activeIndex,
                        totalCount,
                        string.Format("Menjalankan ({0}/{1}): {2}", activeIndex + 1, totalCount, currentItem),
                        lastSent);

                    KeybindItem stepItem = new KeybindItem();
                    stepItem.PrefixVirtualKey = _seqConfig.PrefixVirtualKey;
                    stepItem.TextToSend = textToSend;
                    stepItem.Suffix = _seqConfig.Suffix;

                    _inputSender.SendKeybind(stepItem, _seqConfig.TypingDelayMs, _seqConfig.PrefixDelayMs);

                    lastSent = currentItem;
                    int nextIdx;
                    lock (_lock)
                    {
                        _currentIndex++;
                        nextIdx = _currentIndex;
                        totalCount = _items.Count;
                    }

                    if (nextIdx >= totalCount && !_seqConfig.LoopForever)
                    {
                        completedNaturally = true;
                        break;
                    }

                    PublishProgress(
                        AutoSequenceState.Running,
                        nextIdx < totalCount ? nextIdx : 0,
                        totalCount,
                        string.Format("Selesai kirim '{0}'. Menunggu {1:0.0} dtk...", currentItem, _seqConfig.IntervalMs / 1000.0),
                        lastSent);

                    int waitMs = _seqConfig.IntervalMs < 50 ? 50 : _seqConfig.IntervalMs;
                    SleepInterruptible(waitMs);
                }
            }
            catch (Exception ex)
            {
                PublishProgress(
                    AutoSequenceState.Idle,
                    _currentIndex,
                    _items.Count,
                    "Error Auto Sequence: " + ex.Message,
                    lastSent);
            }
            finally
            {
                _hookService.DisableAutoSequenceHook();

                if (completedNaturally)
                {
                    PublishProgress(
                        AutoSequenceState.Completed,
                        _items.Count,
                        _items.Count,
                        string.Format("Selesai! Seluruh {0} item telah dijalankan.", _items.Count),
                        lastSent);
                }
                else
                {
                    PublishProgress(
                        AutoSequenceState.Idle,
                        _currentIndex < _items.Count ? _currentIndex : 0,
                        _items.Count,
                        "Dihentikan. Terakhir dikirim: " + (string.IsNullOrEmpty(lastSent) ? "-" : lastSent),
                        lastSent);
                }
            }
        }

        private void WaitWhilePaused(string lastSent)
        {
            int idxSnapshot;
            int totalSnapshot;
            string nextItem = "-";
            lock (_lock)
            {
                idxSnapshot = _currentIndex;
                totalSnapshot = _items.Count;
                if (idxSnapshot >= 0 && idxSnapshot < totalSnapshot)
                {
                    nextItem = _items[idxSnapshot];
                }
            }

            PublishProgress(
                AutoSequenceState.Paused,
                idxSnapshot,
                totalSnapshot,
                string.Format("DIJEDA (PAUSE) — Berikutnya: {0} ({1}/{2})", nextItem, idxSnapshot + 1, totalSnapshot),
                lastSent);

            while (_pauseRequested && !_stopRequested)
            {
                Thread.Sleep(50);
            }

            if (!_stopRequested && _resumeCountdownPending)
            {
                _resumeCountdownPending = false;
                int countdown = _seqConfig.CountdownSeconds;
                for (int sec = countdown; sec > 0 && !_stopRequested && !_pauseRequested; sec--)
                {
                    PublishProgress(
                        AutoSequenceState.Countdown,
                        _currentIndex,
                        _items.Count,
                        string.Format("Melanjutkan dalam {0} detik... (arahkan ke jendela target)", sec),
                        lastSent);

                    if (!SleepInterruptible(1000))
                    {
                        break;
                    }
                }
            }
        }

        private bool SleepInterruptible(int totalMs)
        {
            int elapsed = 0;
            while (elapsed < totalMs)
            {
                if (_stopRequested || _pauseRequested)
                {
                    return false;
                }
                int chunk = Math.Min(50, totalMs - elapsed);
                Thread.Sleep(chunk);
                elapsed += chunk;
            }
            return true;
        }

        private bool IsTargetWindowFocused()
        {
            WindowInfo fg = _windowService.GetForegroundWindowInfo();
            return KeybindMatcher.IsWindowKeywordMatched(_seqConfig.TargetWindowKeyword, fg);
        }

        private void PublishProgress(AutoSequenceState state, int idx, int total, string message, string lastSent)
        {
            lock (_lock)
            {
                _state = state;
            }

            Action<AutoSequenceProgressEventArgs> handler = ProgressChanged;
            if (handler != null)
            {
                handler(new AutoSequenceProgressEventArgs
                {
                    State = state,
                    CurrentIndex = idx,
                    TotalCount = total,
                    StatusMessage = message,
                    LastSentItem = lastSent
                });
            }
        }

        private static AutoSequenceConfig CloneConfig(AutoSequenceConfig src)
        {
            if (src == null) return new AutoSequenceConfig();
            AutoSequenceConfig c = new AutoSequenceConfig();
            c.PatternMode = src.PatternMode;
            c.PrefixText = src.PrefixText;
            c.SuffixText = src.SuffixText;
            c.LetterStart = src.LetterStart;
            c.LetterEnd = src.LetterEnd;
            c.NumberStart = src.NumberStart;
            c.NumberEnd = src.NumberEnd;
            c.NumberPadDigits = src.NumberPadDigits;
            c.CustomItemsText = src.CustomItemsText;
            c.CommandTemplate = src.CommandTemplate;
            c.PrefixVirtualKey = src.PrefixVirtualKey;
            c.PrefixDelayMs = src.PrefixDelayMs;
            c.Suffix = src.Suffix;
            c.TypingDelayMs = src.TypingDelayMs;
            c.IntervalMs = src.IntervalMs;
            c.CountdownSeconds = src.CountdownSeconds;
            c.LoopForever = src.LoopForever;
            c.RequireTargetWindowFocus = src.RequireTargetWindowFocus;
            c.TargetWindowKeyword = src.TargetWindowKeyword;
            c.PauseResumeVirtualKey = src.PauseResumeVirtualKey;
            c.PauseResumeCtrl = src.PauseResumeCtrl;
            c.PauseResumeAlt = src.PauseResumeAlt;
            c.PauseResumeShift = src.PauseResumeShift;
            c.StopVirtualKey = src.StopVirtualKey;
            c.StopCtrl = src.StopCtrl;
            c.StopAlt = src.StopAlt;
            c.StopShift = src.StopShift;
            return c;
        }

        public void Dispose()
        {
            _hookService.AutoSequencePauseToggleRequested -= OnHookPauseToggleRequested;
            _hookService.AutoSequenceStopRequested -= OnHookStopRequested;
            Stop();
        }
    }
}
