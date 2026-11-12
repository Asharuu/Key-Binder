using System;
using System.Collections.Generic;

namespace SafeKeyBinder.Models
{
    public enum BinderStatus
    {
        Stopped = 0,
        Active = 1,
        PausedByMaster = 2,
        PausedByTyping = 3
    }

    [Serializable]
    public class AppConfig
    {
        public TargetScopeMode TargetScope { get; set; }
        public string WindowFilterKeyword { get; set; }

        public int MasterToggleVirtualKey { get; set; }
        public bool MasterToggleCtrl { get; set; }
        public bool MasterToggleAlt { get; set; }
        public bool MasterToggleShift { get; set; }

        public bool TypingPauseEnabled { get; set; }
        public int TypingPauseVirtualKey { get; set; }

        public int TypingDelayMs { get; set; }
        public int ManualPrefixDelayMs { get; set; }
        public List<KeybindItem> Keybinds { get; set; }
        public AutoSequenceConfig AutoSequence { get; set; }

        public AppConfig()
        {
            TargetScope = TargetScopeMode.Global;
            WindowFilterKeyword = string.Empty;

            // Default Master Toggle: Pause/Break key
            MasterToggleVirtualKey = 0x13; // VK_PAUSE
            MasterToggleCtrl = false;
            MasterToggleAlt = false;
            MasterToggleShift = false;

            // Generic typing pause is OFF by default for universal workflow
            TypingPauseEnabled = false;
            TypingPauseVirtualKey = 0x54; // 'T' (user-configurable)

            TypingDelayMs = 10;
            ManualPrefixDelayMs = 80;
            Keybinds = new List<KeybindItem>();
            AutoSequence = new AutoSequenceConfig();
        }

        public static AppConfig CreateDefault()
        {
            AppConfig config = new AppConfig();

            KeybindItem sampleGreeting = new KeybindItem();
            sampleGreeting.Label = "Contoh Salam Cepat";
            sampleGreeting.VirtualKey = 0x77; // F8
            sampleGreeting.TextToSend = "Halo! Ini contoh teks otomatis dari SafeKeyBinder.";
            sampleGreeting.Suffix = SuffixAction.None;
            sampleGreeting.Enabled = true;

            KeybindItem sampleSignOff = new KeybindItem();
            sampleSignOff.Label = "Contoh Penutup + Enter";
            sampleSignOff.VirtualKey = 0x78; // F9
            sampleSignOff.RequireCtrl = true;
            sampleSignOff.TextToSend = "Terima kasih, semoga membantu!";
            sampleSignOff.Suffix = SuffixAction.Enter;
            sampleSignOff.Enabled = true;

            config.Keybinds.Add(sampleGreeting);
            config.Keybinds.Add(sampleSignOff);
            return config;
        }
    }
}
