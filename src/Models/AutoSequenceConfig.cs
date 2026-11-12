using System;
using System.Collections.Generic;

namespace SafeKeyBinder.Models
{
    public enum SequencePatternMode
    {
        LetterRange = 0,
        NumberRange = 1,
        CustomList = 2
    }

    public enum AutoSequenceState
    {
        Idle = 0,
        Countdown = 1,
        Running = 2,
        Paused = 3,
        WaitingWindow = 4,
        Completed = 5
    }

    [Serializable]
    public class AutoSequenceConfig
    {
        public SequencePatternMode PatternMode { get; set; }

        // Letter pattern settings (e.g. "77" + "a".."z" -> 77a..77z)
        public string PrefixText { get; set; }
        public string SuffixText { get; set; }
        public string LetterStart { get; set; }
        public string LetterEnd { get; set; }

        // Number pattern settings (e.g. "world" + 1..50)
        public int NumberStart { get; set; }
        public int NumberEnd { get; set; }
        public int NumberPadDigits { get; set; }

        // Manual custom list (one item per line)
        public string CustomItemsText { get; set; }

        // Execution & Command template
        public string CommandTemplate { get; set; }
        public int PrefixVirtualKey { get; set; }
        public int PrefixDelayMs { get; set; }
        public SuffixAction Suffix { get; set; }
        public int TypingDelayMs { get; set; }
        public int IntervalMs { get; set; }
        public int CountdownSeconds { get; set; }
        public bool LoopForever { get; set; }

        // Safety Window Guard
        public bool RequireTargetWindowFocus { get; set; }
        public string TargetWindowKeyword { get; set; }

        // Emergency / Pause Hotkeys
        public int PauseResumeVirtualKey { get; set; }
        public bool PauseResumeCtrl { get; set; }
        public bool PauseResumeAlt { get; set; }
        public bool PauseResumeShift { get; set; }

        public int StopVirtualKey { get; set; }
        public bool StopCtrl { get; set; }
        public bool StopAlt { get; set; }
        public bool StopShift { get; set; }

        public AutoSequenceConfig()
        {
            PatternMode = SequencePatternMode.LetterRange;
            PrefixText = "77";
            SuffixText = string.Empty;
            LetterStart = "a";
            LetterEnd = "z";

            NumberStart = 1;
            NumberEnd = 50;
            NumberPadDigits = 0;

            CustomItemsText = "77a\r\n77b\r\n77c";

            CommandTemplate = "warp {item}";
            PrefixVirtualKey = 0xBF; // '/'
            PrefixDelayMs = 120;
            Suffix = SuffixAction.Enter;
            TypingDelayMs = 10;
            IntervalMs = 3500;
            CountdownSeconds = 3;
            LoopForever = false;

            RequireTargetWindowFocus = false;
            TargetWindowKeyword = string.Empty;

            PauseResumeVirtualKey = 0x75; // F6
            PauseResumeCtrl = false;
            PauseResumeAlt = false;
            PauseResumeShift = false;

            StopVirtualKey = 0x76; // F7
            StopCtrl = false;
            StopAlt = false;
            StopShift = false;
        }
    }
}
