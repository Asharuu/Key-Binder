using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SafeKeyBinder.UI
{
    public class SecurityAuditDialog : Form
    {
        private readonly string _sourceFolderPath;
        private readonly string _configPath;

        public SecurityAuditDialog(string sourceFolderPath, string configPath)
        {
            _sourceFolderPath = sourceFolderPath;
            _configPath = configPath;
            InitializeUi();
        }

        private void InitializeUi()
        {
            this.Text = "Jaminan Keamanan & Transparansi Kode";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ThemeTokens.BgSurface;
            this.Font = ThemeTokens.FontBody;
            this.ClientSize = new Size(ThemeTokens.Scale(560), ThemeTokens.Scale(420));
            this.Padding = ThemeTokens.ScaledPadding(16, 16, 16, 16);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "Mengapa SafeKeyBinder Aman Digunakan?";
            lblTitle.Font = ThemeTokens.FontTitle;
            lblTitle.ForeColor = ThemeTokens.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 10);

            TextBox txtReport = new TextBox();
            txtReport.Multiline = true;
            txtReport.ReadOnly = true;
            txtReport.ScrollBars = ScrollBars.Vertical;
            txtReport.BackColor = ThemeTokens.BgSubtle;
            txtReport.ForeColor = ThemeTokens.TextPrimary;
            txtReport.Font = ThemeTokens.FontBody;
            txtReport.BorderStyle = BorderStyle.FixedSingle;
            txtReport.Dock = DockStyle.Fill;
            txtReport.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 12);
            txtReport.Text =
                "1. 100% OFFLINE (TIDAK ADA AKSES INTERNET)\r\n" +
                "   Aplikasi ini tidak mengimpor System.Net, HttpClient, WebClient, maupun API jaringan apa pun.\r\n\r\n" +
                "2. TIDAK ADA KEYLOGGING / PENYIMPANAN KETIKAN\r\n" +
                "   Hook Windows (WH_KEYBOARD_LL) hanya memeriksa di memori RAM apakah tombol cocok dengan daftar hotkey Anda.\r\n" +
                "   Ketikan normal (password, chat, dokumen) langsung dilewati dan TIDAK PERNAH dicatat ke file.\r\n\r\n" +
                "3. TIDAK MENGGANGGU CLIPBOARD\r\n" +
                "   Teks dikirim via Win32 SendInput (KEYEVENTF_UNICODE), sehingga data copy-paste Anda tetap aman.\r\n\r\n" +
                "4. NOL LIBRARY EKSTERNAL & BISA DI-COMPILE ULANG SENDIRI\r\n" +
                "   Hanya memakai DLL bawaan Windows (.NET Framework & user32.dll). Anda bisa membaca seluruh kode di folder src\\ lalu menjalankan build.bat untuk memverifikasi isi .exe.\r\n\r\n" +
                "Lokasi Konfigurasi Lokal:\r\n" + _configPath;

            TableLayoutPanel btnBar = new TableLayoutPanel();
            btnBar.AutoSize = true;
            btnBar.Dock = DockStyle.Fill;
            btnBar.ColumnCount = 2;
            btnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            btnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            Button btnOpenSrc = new Button();
            btnOpenSrc.Text = "Buka Folder Source Code";
            ThemeTokens.StyleSecondaryButton(btnOpenSrc);
            btnOpenSrc.Anchor = AnchorStyles.Left;
            btnOpenSrc.Click += delegate
            {
                try
                {
                    if (Directory.Exists(_sourceFolderPath))
                    {
                        Process.Start("explorer.exe", _sourceFolderPath);
                    }
                }
                catch
                {
                    MessageBox.Show("Folder source: " + _sourceFolderPath, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Button btnClose = new Button();
            btnClose.Text = "Tutup";
            ThemeTokens.StylePrimaryButton(btnClose);
            btnClose.Anchor = AnchorStyles.Right;
            btnClose.Click += delegate { this.Close(); };

            btnBar.Controls.Add(btnOpenSrc, 0, 0);
            btnBar.Controls.Add(btnClose, 1, 0);

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(txtReport, 0, 1);
            layout.Controls.Add(btnBar, 0, 2);

            this.Controls.Add(layout);
        }
    }
}
