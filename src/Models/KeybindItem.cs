using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SafeKeyBinder.Models
{
    public enum SuffixAction
    {
        None = 0,
        Enter = 1,
        Tab = 2,
        Space = 3
    }

    public enum TargetScopeMode
    {
        Global = 0,
        WindowFilter = 1
    }

    [Serializable]
    public class KeybindItem
    {
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public string Label { get; set; }
        public int VirtualKey { get; set; }
        public bool RequireCtrl { get; set; }
        public bool RequireAlt { get; set; }
        public bool RequireShift { get; set; }
        public string TextToSend { get; set; }
        public int PrefixVirtualKey { get; set; }
        public SuffixAction Suffix { get; set; }

        public KeybindItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Enabled = true;
            Label = string.Empty;
            VirtualKey = 0;
            RequireCtrl = false;
            RequireAlt = false;
            RequireShift = false;
            TextToSend = string.Empty;
            PrefixVirtualKey = 0;
            Suffix = SuffixAction.None;
        }

        public KeybindItem Clone()
        {
            KeybindItem copy = new KeybindItem();
            copy.Id = this.Id;
            copy.Enabled = this.Enabled;
            copy.Label = this.Label;
            copy.VirtualKey = this.VirtualKey;
            copy.RequireCtrl = this.RequireCtrl;
            copy.RequireAlt = this.RequireAlt;
            copy.RequireShift = this.RequireShift;
            copy.TextToSend = this.TextToSend;
            copy.PrefixVirtualKey = this.PrefixVirtualKey;
            copy.Suffix = this.Suffix;
            return copy;
        }

        public string GetHotkeyDisplay()
        {
            return FormatHotkey(VirtualKey, RequireCtrl, RequireAlt, RequireShift);
        }

        public string GetPrefixDisplay()
        {
            if (PrefixVirtualKey <= 0)
            {
                return "-";
            }
            return FormatKeyName(PrefixVirtualKey);
        }

        public string GetSuffixDisplay()
        {
            switch (Suffix)
            {
                case SuffixAction.Enter:
                    return "Enter";
                case SuffixAction.Tab:
                    return "Tab";
                case SuffixAction.Space:
                    return "Space";
                default:
                    return "Tidak ada";
            }
        }

        public static string FormatHotkey(int vk, bool ctrl, bool alt, bool shift)
        {
            if (vk <= 0)
            {
                return "(Belum diatur)";
            }

            List<string> parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (alt) parts.Add("Alt");
            if (shift) parts.Add("Shift");
            parts.Add(FormatKeyName(vk));
            return string.Join(" + ", parts.ToArray());
        }

        public static string FormatKeyName(int vk)
        {
            if (vk <= 0) return "(Kosong)";

            switch (vk)
            {
                case 0x0D: return "Enter";
                case 0x1B: return "Esc";
                case 0x09: return "Tab";
                case 0x20: return "Space";
                case 0x08: return "Backspace";
                case 0x13: return "Pause";
                case 0x91: return "ScrollLock";
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0x24: return "Home";
                case 0x23: return "End";
                case 0x21: return "PageUp";
                case 0x22: return "PageDown";
                case 0x26: return "Up";
                case 0x28: return "Down";
                case 0x25: return "Left";
                case 0x27: return "Right";
                case 0xBD: return "-";
                case 0xBB: return "=";
                case 0xDB: return "[";
                case 0xDD: return "]";
                case 0xDC: return "\\";
                case 0xBA: return ";";
                case 0xDE: return "'";
                case 0xBC: return ",";
                case 0xBE: return ".";
                case 0xBF: return "/";
                case 0xC0: return "`";
                case 0x6A: return "Numpad *";
                case 0x6B: return "Numpad +";
                case 0x6D: return "Numpad -";
                case 0x6E: return "Numpad .";
                case 0x6F: return "Numpad /";
            }

            if (vk >= 0x30 && vk <= 0x39)
            {
                return ((char)vk).ToString();
            }

            if (vk >= 0x41 && vk <= 0x5A)
            {
                return ((char)vk).ToString();
            }

            if (vk >= 0x60 && vk <= 0x69)
            {
                return "Numpad " + (vk - 0x60);
            }

            if (vk >= 0x70 && vk <= 0x87)
            {
                return "F" + (vk - 0x6F);
            }

            Keys key = (Keys)vk;
            return key.ToString();
        }
    }
}
