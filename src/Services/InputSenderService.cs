using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SafeKeyBinder.Models;
using SafeKeyBinder.Native;

namespace SafeKeyBinder.Services
{
    public class InputSenderService
    {
        private DateTime _lastModifierReleaseUtc = DateTime.MinValue;
        private readonly object _timeLock = new object();
        private readonly object _sendLock = new object();

        public DateTime LastModifierReleaseUtc
        {
            get
            {
                lock (_timeLock)
                {
                    return _lastModifierReleaseUtc;
                }
            }
        }

        public uint SendKeybind(KeybindItem item, int typingDelayMs)
        {
            return SendKeybind(item, typingDelayMs, 80);
        }

        public uint SendKeybind(KeybindItem item, int typingDelayMs, int prefixDelayMs)
        {
            if (item == null)
            {
                return 0;
            }

            lock (_sendLock)
            {
                uint totalSent = 0;

                bool releasedAny = ReleaseHeldModifiers();
                if (releasedAny)
                {
                    Thread.Sleep(25);
                }

                if (item.PrefixVirtualKey > 0)
                {
                    totalSent += SendVirtualKey(item.PrefixVirtualKey);
                    Thread.Sleep(prefixDelayMs > 0 ? prefixDelayMs : 60);
                }

                if (!string.IsNullOrEmpty(item.TextToSend))
                {
                    totalSent += SendText(item.TextToSend, typingDelayMs);
                }

                int suffixVk = GetSuffixVirtualKey(item.Suffix);
                if (suffixVk > 0)
                {
                    Thread.Sleep(40);
                    totalSent += SendVirtualKey(suffixVk);
                }

                return totalSent;
            }
        }

        public uint SendText(string text, int typingDelayMs)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            uint totalSent = 0;
            // Game engines (such as Roblox) often drop letters if multiple KEYEVENTF_UNICODE
            // events arrive in the exact same frame (0ms burst). Keep a small minimum gap.
            int interCharDelayMs = typingDelayMs > 0
                ? typingDelayMs
                : (text.Length > 80 ? 1 : 5);

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '\r') continue;

                if (ch == '\n')
                {
                    totalSent += SendVirtualKey(Win32Native.VK_RETURN);
                }
                else if (ch == '\t')
                {
                    totalSent += SendVirtualKey(Win32Native.VK_TAB);
                }
                else
                {
                    totalSent += SendUnicodeChar(ch);
                }

                if (i + 1 < text.Length)
                {
                    Thread.Sleep(interCharDelayMs);
                }
            }

            return totalSent;
        }

        public uint SendVirtualKey(int vk)
        {
            if (vk <= 0) return 0;

            uint sent = 0;
            sent += DispatchSingle(CreateVirtualKeyInput(vk, false));
            // Hold 35ms (~2 frames at 60 FPS) so game engines (Roblox/DirectInput) detect key down
            Thread.Sleep(35);
            sent += DispatchSingle(CreateVirtualKeyInput(vk, true));
            return sent;
        }

        private uint SendUnicodeChar(char ch)
        {
            uint sent = 0;
            sent += DispatchSingle(CreateUnicodeInput(ch, false));
            Thread.Sleep(5);
            sent += DispatchSingle(CreateUnicodeInput(ch, true));
            return sent;
        }

        public bool ReleaseHeldModifiers()
        {
            int[] modifierVks = new int[]
            {
                Win32Native.VK_LCONTROL,
                Win32Native.VK_RCONTROL,
                Win32Native.VK_LSHIFT,
                Win32Native.VK_RSHIFT,
                Win32Native.VK_LMENU,
                Win32Native.VK_RMENU,
                Win32Native.VK_LWIN,
                Win32Native.VK_RWIN
            };

            List<Win32Native.INPUT> ups = new List<Win32Native.INPUT>();
            for (int i = 0; i < modifierVks.Length; i++)
            {
                int vk = modifierVks[i];
                if ((Win32Native.GetAsyncKeyState(vk) & 0x8000) != 0)
                {
                    ups.Add(CreateVirtualKeyInput(vk, true));
                }
            }

            if (ups.Count == 0)
            {
                return false;
            }

            lock (_timeLock)
            {
                _lastModifierReleaseUtc = DateTime.UtcNow;
            }

            DispatchInputs(ups);
            return true;
        }

        private static int GetSuffixVirtualKey(SuffixAction suffix)
        {
            switch (suffix)
            {
                case SuffixAction.Enter: return Win32Native.VK_RETURN;
                case SuffixAction.Tab: return Win32Native.VK_TAB;
                case SuffixAction.Space: return Win32Native.VK_SPACE;
                default: return 0;
            }
        }

        private static Win32Native.INPUT CreateUnicodeInput(char ch, bool keyUp)
        {
            Win32Native.INPUT input = new Win32Native.INPUT();
            input.type = Win32Native.INPUT_KEYBOARD;
            input.U.ki.wVk = 0;
            input.U.ki.wScan = (ushort)ch;
            input.U.ki.dwFlags = Win32Native.KEYEVENTF_UNICODE | (keyUp ? Win32Native.KEYEVENTF_KEYUP : 0);
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }

        private static Win32Native.INPUT CreateVirtualKeyInput(int vk, bool keyUp)
        {
            uint flags = keyUp ? Win32Native.KEYEVENTF_KEYUP : 0;
            if (IsExtendedKey(vk))
            {
                flags |= Win32Native.KEYEVENTF_EXTENDEDKEY;
            }

            ushort scan = (ushort)Win32Native.MapVirtualKey((uint)vk, Win32Native.MAPVK_VK_TO_VSC);
            if (scan != 0)
            {
                flags |= Win32Native.KEYEVENTF_SCANCODE;
            }

            Win32Native.INPUT input = new Win32Native.INPUT();
            input.type = Win32Native.INPUT_KEYBOARD;
            input.U.ki.wVk = scan != 0 ? (ushort)0 : (ushort)vk;
            input.U.ki.wScan = scan;
            input.U.ki.dwFlags = flags;
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }

        private static bool IsExtendedKey(int vk)
        {
            switch (vk)
            {
                case Win32Native.VK_RCONTROL:
                case Win32Native.VK_RMENU:
                case Win32Native.VK_LWIN:
                case Win32Native.VK_RWIN:
                case 0x21: // PageUp
                case 0x22: // PageDown
                case 0x23: // End
                case 0x24: // Home
                case 0x25: // Left
                case 0x26: // Up
                case 0x27: // Right
                case 0x28: // Down
                case 0x2D: // Insert
                case 0x2E: // Delete
                case 0x6F: // Numpad /
                    return true;
                default:
                    return false;
            }
        }

        private static uint DispatchSingle(Win32Native.INPUT singleInput)
        {
            Win32Native.INPUT[] arr = new Win32Native.INPUT[] { singleInput };
            return Win32Native.SendInput(1, arr, Marshal.SizeOf(typeof(Win32Native.INPUT)));
        }

        private static uint DispatchInputs(List<Win32Native.INPUT> list)
        {
            if (list == null || list.Count == 0)
            {
                return 0;
            }

            Win32Native.INPUT[] arr = list.ToArray();
            return Win32Native.SendInput((uint)arr.Length, arr, Marshal.SizeOf(typeof(Win32Native.INPUT)));
        }
    }
}
