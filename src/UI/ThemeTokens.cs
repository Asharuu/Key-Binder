using System.Drawing;
using System.Windows.Forms;

namespace SafeKeyBinder.UI
{
    /// <summary>
    /// Centralized HIG design tokens: colors, typography hierarchy, and DPI-safe AutoSize helpers.
    /// </summary>
    public static class ThemeTokens
    {
        // Surface & Background Colors
        public static readonly Color BgApp = Color.FromArgb(248, 250, 252);       // #F8FAFC
        public static readonly Color BgSurface = Color.FromArgb(255, 255, 255);   // #FFFFFF
        public static readonly Color BgSubtle = Color.FromArgb(241, 245, 249);    // #F1F5F9
        public static readonly Color BorderSubtle = Color.FromArgb(226, 232, 240);// #E2E8F0
        public static readonly Color BorderStrong = Color.FromArgb(203, 213, 225);// #CBD5E1

        // Text Hierarchy
        public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);    // #0F172A
        public static readonly Color TextSecondary = Color.FromArgb(71, 85, 105); // #475569
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);   // #64748B

        // Brand & Semantic Accents
        public static readonly Color AccentPrimary = Color.FromArgb(37, 99, 235);     // #2563EB
        public static readonly Color SuccessBg = Color.FromArgb(220, 252, 231);       // #DCFCE7
        public static readonly Color SuccessText = Color.FromArgb(21, 128, 61);       // #15803D
        public static readonly Color WarningBg = Color.FromArgb(254, 243, 199);       // #FEF3C7
        public static readonly Color WarningText = Color.FromArgb(180, 83, 9);        // #B45309
        public static readonly Color DangerPrimary = Color.FromArgb(220, 38, 38);     // #DC2626
        public static readonly Color NeutralBadgeBg = Color.FromArgb(226, 232, 240);  // #E2E8F0

        // Typography Scale (3+ distinct levels)
        public static readonly Font FontTitle = new Font("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font FontSection = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font FontBody = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font FontBodyBold = new Font("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font FontCaption = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static readonly Font FontMono = new Font("Consolas", 9.5f, FontStyle.Bold);

        // Base 4px/8px Spacing Scale (scaled at runtime via Scale())
        public const int SpaceXS = 4;
        public const int SpaceSM = 8;
        public const int SpaceMD = 12;
        public const int SpaceLG = 16;

        public static float GetDpiFactor()
        {
            using (Graphics g = Graphics.FromHwnd(System.IntPtr.Zero))
            {
                float factor = g.DpiX / 96f;
                return factor < 1f ? 1f : factor;
            }
        }

        public static int Scale(int basePx)
        {
            return (int)System.Math.Round(basePx * GetDpiFactor());
        }

        public static Padding ScaledPadding(int left, int top, int right, int bottom)
        {
            return new Padding(Scale(left), Scale(top), Scale(right), Scale(bottom));
        }

        public static void StylePrimaryButton(Button btn)
        {
            ConfigureButtonBase(btn, 32, 14);
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = AccentPrimary;
            btn.ForeColor = Color.White;
            btn.Font = FontBodyBold;
        }

        public static void StyleDangerButton(Button btn)
        {
            ConfigureButtonBase(btn, 32, 14);
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = DangerPrimary;
            btn.ForeColor = Color.White;
            btn.Font = FontBodyBold;
        }

        public static void StyleSecondaryButton(Button btn)
        {
            ConfigureButtonBase(btn, 28, 10);
            btn.FlatAppearance.BorderColor = BorderStrong;
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = BgSurface;
            btn.ForeColor = TextPrimary;
            btn.Font = FontBody;
        }

        private static void ConfigureButtonBase(Button btn, int minHeight, int horizPad)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.UseMnemonic = false;
            btn.AutoSize = true;
            btn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn.Padding = ScaledPadding(horizPad, 3, horizPad, 3);
            btn.MinimumSize = new Size(0, Scale(minHeight));
            btn.Cursor = Cursors.Hand;
        }

        private static Icon _cachedAppIcon;

        public static Icon GetAppIcon()
        {
            if (_cachedAppIcon != null)
            {
                return _cachedAppIcon;
            }
            try
            {
                string exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
                {
                    _cachedAppIcon = Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch
            {
            }
            return _cachedAppIcon;
        }
    }
}
