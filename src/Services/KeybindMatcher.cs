using System;
using SafeKeyBinder.Models;
using SafeKeyBinder.Native;

namespace SafeKeyBinder.Services
{
    public enum HookMatchAction
    {
        None = 0,
        TriggerKeybind = 1,
        ToggleMaster = 2,
        TypingPause = 3,
        TypingResume = 4
    }

    public class HookMatchResult
    {
        public HookMatchAction Action { get; set; }
        public KeybindItem MatchedItem { get; set; }
        public bool SuppressKeystroke { get; set; }

        public static HookMatchResult NoMatch()
        {
            return new HookMatchResult
            {
                Action = HookMatchAction.None,
                MatchedItem = null,
                SuppressKeystroke = false
            };
        }
    }

    /// <summary>
    /// Pure matching logic decoupled from Win32 hooks so it can be unit-tested thoroughly.
    /// </summary>
    public static class KeybindMatcher
    {
        public static bool IsTargetWindowMatched(AppConfig config, WindowInfo activeWindow)
        {
            if (config == null || config.TargetScope == TargetScopeMode.Global)
            {
                return true;
            }

            return IsWindowKeywordMatched(config.WindowFilterKeyword, activeWindow);
        }

        public static bool IsWindowKeywordMatched(string rawKeyword, WindowInfo activeWindow)
        {
            string keyword = (rawKeyword ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                return true;
            }

            if (activeWindow == null)
            {
                return false;
            }

            string title = activeWindow.Title ?? string.Empty;
            string proc = activeWindow.ProcessName ?? string.Empty;

            // Support "[proc] title" selection string or plain keyword match
            if (keyword.StartsWith("[") && keyword.Contains("]"))
            {
                int closeIdx = keyword.IndexOf(']');
                string procPart = keyword.Substring(1, closeIdx - 1).Trim();
                string titlePart = keyword.Substring(closeIdx + 1).Trim();

                bool procMatches = !string.IsNullOrEmpty(procPart) &&
                    proc.IndexOf(procPart, StringComparison.OrdinalIgnoreCase) >= 0;
                bool titleMatches = !string.IsNullOrEmpty(titlePart) &&
                    title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0;

                if (procMatches && (string.IsNullOrEmpty(titlePart) || titleMatches))
                {
                    return true;
                }
            }

            return title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   proc.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static HookMatchResult EvaluateKeyDown(
            AppConfig config,
            BinderStatus currentStatus,
            int vkCode,
            bool ctrlDown,
            bool altDown,
            bool shiftDown,
            WindowInfo activeWindow)
        {
            if (config == null || currentStatus == BinderStatus.Stopped)
            {
                return HookMatchResult.NoMatch();
            }

            // 1. Master Toggle Hotkey always works regardless of pause/window filter
            if (config.MasterToggleVirtualKey > 0 &&
                vkCode == config.MasterToggleVirtualKey &&
                ctrlDown == config.MasterToggleCtrl &&
                altDown == config.MasterToggleAlt &&
                shiftDown == config.MasterToggleShift)
            {
                return new HookMatchResult
                {
                    Action = HookMatchAction.ToggleMaster,
                    SuppressKeystroke = true
                };
            }

            if (currentStatus == BinderStatus.PausedByMaster)
            {
                return HookMatchResult.NoMatch();
            }

            // 2. Optional Typing Pause / Resume (never suppresses the typed key itself)
            if (config.TypingPauseEnabled)
            {
                if (currentStatus == BinderStatus.PausedByTyping)
                {
                    if (!ctrlDown && !altDown && !shiftDown &&
                        (vkCode == Win32Native.VK_RETURN || vkCode == Win32Native.VK_ESCAPE))
                    {
                        return new HookMatchResult
                        {
                            Action = HookMatchAction.TypingResume,
                            SuppressKeystroke = false
                        };
                    }
                    return HookMatchResult.NoMatch();
                }

                if (currentStatus == BinderStatus.Active &&
                    config.TypingPauseVirtualKey > 0 &&
                    vkCode == config.TypingPauseVirtualKey &&
                    !ctrlDown && !altDown && !shiftDown &&
                    IsTargetWindowMatched(config, activeWindow))
                {
                    return new HookMatchResult
                    {
                        Action = HookMatchAction.TypingPause,
                        SuppressKeystroke = false
                    };
                }
            }

            if (currentStatus != BinderStatus.Active)
            {
                return HookMatchResult.NoMatch();
            }

            // 3. Window Scope Check
            if (!IsTargetWindowMatched(config, activeWindow))
            {
                return HookMatchResult.NoMatch();
            }

            // 4. Keybind Lookup
            if (config.Keybinds != null)
            {
                for (int i = 0; i < config.Keybinds.Count; i++)
                {
                    KeybindItem item = config.Keybinds[i];
                    if (item == null || !item.Enabled || item.VirtualKey <= 0)
                    {
                        continue;
                    }

                    if (item.VirtualKey == vkCode &&
                        item.RequireCtrl == ctrlDown &&
                        item.RequireAlt == altDown &&
                        item.RequireShift == shiftDown)
                    {
                        return new HookMatchResult
                        {
                            Action = HookMatchAction.TriggerKeybind,
                            MatchedItem = item,
                            SuppressKeystroke = true
                        };
                    }
                }
            }

            return HookMatchResult.NoMatch();
        }
    }
}
