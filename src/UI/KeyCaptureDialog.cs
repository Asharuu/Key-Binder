using System;
using System.Drawing;
using System.Windows.Forms;
using SafeKeyBinder.Models;

namespace SafeKeyBinder.UI
{
    public class KeyCaptureDialog : Form
    {
        private readonly bool _allowModifiers;
        private readonly bool _allowClear;

        private Label _lblInstruction;
        private Label _lblPreview;
        private CheckBox _chkCtrl;
        private CheckBox _chkAlt;
        private CheckBox _chkShift;
        private Button _btnClear;
        private Button _btnCancel;
        private Button _btnConfirm;

        public int SelectedVirtualKey { get; private set; }
        public bool SelectedCtrl { get; private set; }
        public bool SelectedAlt { get; private set; }
        public bool SelectedShift { get; private set; }

        public KeyCaptureDialog(
            string title,
            int initialVk,
            bool initialCtrl,
            bool initialAlt,
            bool initialShift,
            bool allowModifiers,
            bool allowClear)
        {
            _allowModifiers = allowModifiers;
            _allowClear = allowClear;

            SelectedVirtualKey = initialVk;
            SelectedCtrl = allowModifiers && initialCtrl;
            SelectedAlt = allowModifiers && initialAlt;
            SelectedShift = allowModifiers && initialShift;

            InitializeUi(title);
            RefreshPreview();
        }

        private void InitializeUi(string title)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = ThemeTokens.BgSurface;
            this.Font = ThemeTokens.FontBody;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimumSize = new Size(ThemeTokens.Scale(420), 0);
            this.Padding = ThemeTokens.ScaledPadding(16, 16, 16, 16);
            this.KeyPreview = true;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _lblInstruction = new Label();
            _lblInstruction.UseMnemonic = false;
            _lblInstruction.Text = "Tekan tombol keyboard yang ingin digunakan sekarang:";
            _lblInstruction.Font = ThemeTokens.FontBody;
            _lblInstruction.ForeColor = ThemeTokens.TextSecondary;
            _lblInstruction.AutoSize = true;
            _lblInstruction.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            _lblPreview = new Label();
            _lblPreview.UseMnemonic = false;
            _lblPreview.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            _lblPreview.ForeColor = ThemeTokens.AccentPrimary;
            _lblPreview.BackColor = ThemeTokens.BgSubtle;
            _lblPreview.BorderStyle = BorderStyle.FixedSingle;
            _lblPreview.TextAlign = ContentAlignment.MiddleCenter;
            _lblPreview.MinimumSize = new Size(ThemeTokens.Scale(380), ThemeTokens.Scale(52));
            _lblPreview.Dock = DockStyle.Fill;
            _lblPreview.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 12);

            layout.Controls.Add(_lblInstruction);
            layout.Controls.Add(_lblPreview);

            if (_allowModifiers)
            {
                FlowLayoutPanel modFlow = new FlowLayoutPanel();
                modFlow.AutoSize = true;
                modFlow.WrapContents = false;
                modFlow.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 12);

                _chkCtrl = new CheckBox();
                _chkCtrl.Text = "Ctrl";
                _chkCtrl.AutoSize = true;
                _chkCtrl.Checked = SelectedCtrl;
                _chkCtrl.Margin = ThemeTokens.ScaledPadding(0, 0, 14, 0);
                _chkCtrl.CheckedChanged += delegate
                {
                    SelectedCtrl = _chkCtrl.Checked;
                    RefreshPreview();
                };

                _chkAlt = new CheckBox();
                _chkAlt.Text = "Alt";
                _chkAlt.AutoSize = true;
                _chkAlt.Checked = SelectedAlt;
                _chkAlt.Margin = ThemeTokens.ScaledPadding(0, 0, 14, 0);
                _chkAlt.CheckedChanged += delegate
                {
                    SelectedAlt = _chkAlt.Checked;
                    RefreshPreview();
                };

                _chkShift = new CheckBox();
                _chkShift.Text = "Shift";
                _chkShift.AutoSize = true;
                _chkShift.Checked = SelectedShift;
                _chkShift.CheckedChanged += delegate
                {
                    SelectedShift = _chkShift.Checked;
                    RefreshPreview();
                };

                modFlow.Controls.Add(_chkCtrl);
                modFlow.Controls.Add(_chkAlt);
                modFlow.Controls.Add(_chkShift);
                layout.Controls.Add(modFlow);
            }

            TableLayoutPanel btnBar = new TableLayoutPanel();
            btnBar.AutoSize = true;
            btnBar.Dock = DockStyle.Fill;
            btnBar.ColumnCount = 2;
            btnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            btnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            btnBar.Margin = ThemeTokens.ScaledPadding(0, 4, 0, 0);

            FlowLayoutPanel leftFlow = new FlowLayoutPanel();
            leftFlow.AutoSize = true;
            leftFlow.WrapContents = false;
            leftFlow.Dock = DockStyle.Left;

            if (_allowClear)
            {
                _btnClear = new Button();
                _btnClear.Text = "Kosongkan";
                ThemeTokens.StyleSecondaryButton(_btnClear);
                _btnClear.Click += delegate
                {
                    SelectedVirtualKey = 0;
                    SelectedCtrl = false;
                    SelectedAlt = false;
                    SelectedShift = false;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                leftFlow.Controls.Add(_btnClear);
            }

            FlowLayoutPanel rightFlow = new FlowLayoutPanel();
            rightFlow.AutoSize = true;
            rightFlow.WrapContents = false;
            rightFlow.FlowDirection = FlowDirection.RightToLeft;
            rightFlow.Dock = DockStyle.Right;

            _btnConfirm = new Button();
            _btnConfirm.Text = "Pakai Tombol";
            ThemeTokens.StylePrimaryButton(_btnConfirm);
            _btnConfirm.Margin = ThemeTokens.ScaledPadding(8, 0, 0, 0);
            _btnConfirm.Click += delegate
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _btnCancel = new Button();
            _btnCancel.Text = "Batal";
            ThemeTokens.StyleSecondaryButton(_btnCancel);
            _btnCancel.MinimumSize = new Size(0, ThemeTokens.Scale(32));
            _btnCancel.Click += delegate
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            rightFlow.Controls.Add(_btnConfirm);
            rightFlow.Controls.Add(_btnCancel);

            btnBar.Controls.Add(leftFlow, 0, 0);
            btnBar.Controls.Add(rightFlow, 1, 0);

            layout.Controls.Add(btnBar);
            this.Controls.Add(layout);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;
            if (IsModifierOnly(keyCode))
            {
                return true;
            }

            int vk = (int)keyCode;
            if (vk > 0)
            {
                SelectedVirtualKey = vk;
                if (_allowModifiers)
                {
                    SelectedCtrl = (keyData & Keys.Control) == Keys.Control;
                    SelectedAlt = (keyData & Keys.Alt) == Keys.Alt;
                    SelectedShift = (keyData & Keys.Shift) == Keys.Shift;

                    if (_chkCtrl != null) _chkCtrl.Checked = SelectedCtrl;
                    if (_chkAlt != null) _chkAlt.Checked = SelectedAlt;
                    if (_chkShift != null) _chkShift.Checked = SelectedShift;
                }
                else
                {
                    SelectedCtrl = false;
                    SelectedAlt = false;
                    SelectedShift = false;
                }

                RefreshPreview();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static bool IsModifierOnly(Keys key)
        {
            switch (key)
            {
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.LWin:
                case Keys.RWin:
                case Keys.None:
                    return true;
                default:
                    return false;
            }
        }

        private void RefreshPreview()
        {
            if (SelectedVirtualKey <= 0)
            {
                _lblPreview.Text = "(Menunggu tombol...)";
                _btnConfirm.Enabled = false;
            }
            else
            {
                _lblPreview.Text = _allowModifiers
                    ? KeybindItem.FormatHotkey(SelectedVirtualKey, SelectedCtrl, SelectedAlt, SelectedShift)
                    : KeybindItem.FormatKeyName(SelectedVirtualKey);
                _btnConfirm.Enabled = true;
            }
        }
    }
}
