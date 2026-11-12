using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using SafeKeyBinder.Models;
using SafeKeyBinder.Services;

namespace SafeKeyBinder.UI
{
    public class MainForm : Form
    {
        private readonly ConfigService _configService;
        private readonly WindowService _windowService;
        private readonly InputSenderService _inputSender;
        private readonly KeyboardHookService _hookService;
        private readonly AutoSequenceService _autoService;

        private AppConfig _config;
        private string _editingItemId = null;
        private int _editorVirtualKey = 0;
        private bool _editorCtrl = false;
        private bool _editorAlt = false;
        private bool _editorShift = false;
        private int _editorPrefixVk = 0;

        private bool _isPopulatingList = false;
        private bool _isLoadingSettings = false;

        // Header Controls
        private Label _lblStatusBadge;
        private Button _btnAdminRestart;
        private Button _btnSecurityInfo;
        private Button _btnStartStop;

        // Left Panel (List)
        private ListView _lvKeybinds;
        private Button _btnAddNew;
        private Button _btnDuplicate;
        private Button _btnDelete;

        // Right Panel - Editor
        private Label _lblEditorMode;
        private TextBox _txtLabel;
        private TextBox _txtHotkeyPreview;
        private Button _btnRecordHotkey;
        private TextBox _txtTextToSend;
        private TextBox _txtPrefixPreview;
        private Button _btnRecordPrefix;
        private ComboBox _cmbSuffix;
        private CheckBox _chkEnabled;
        private Button _btnResetEditor;
        private Button _btnSaveItem;

        // Right Panel - Universal Settings
        private RadioButton _rbScopeGlobal;
        private RadioButton _rbScopeWindow;
        private ComboBox _cmbWindowTarget;
        private Button _btnRefreshWindows;
        private TextBox _txtMasterToggle;
        private Button _btnRecordMaster;
        private CheckBox _chkTypingPause;
        private TextBox _txtTypingPauseKey;
        private Button _btnRecordTypingPause;
        private NumericUpDown _numTypingDelay;

        // Right Panel - Test Box & Bottom Status
        private TextBox _txtTestArea;
        private Label _lblFooterStatus;
        private TabControl _tabMain;
        private AutoSequenceControl _autoSequenceControl;

        public MainForm(
            ConfigService configService,
            WindowService windowService,
            InputSenderService inputSender,
            KeyboardHookService hookService,
            AutoSequenceService autoService)
        {
            _configService = configService;
            _windowService = windowService;
            _inputSender = inputSender;
            _hookService = hookService;
            _autoService = autoService;

            _config = _configService.Load();

            InitializeUi();
            WireEvents();

            LoadSettingsIntoUi();
            PopulateKeybindList();
            PrepareNewItemEditor();
            RefreshOpenWindowsList();
            UpdateStatusUi(BinderStatus.Stopped);
        }

        private void InitializeUi()
        {
            this.Text = "SafeKeyBinder — Universal Windows KeyBinder";
            Icon appIcon = ThemeTokens.GetAppIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }
            this.BackColor = ThemeTokens.BgApp;
            this.Font = ThemeTokens.FontBody;
            this.StartPosition = FormStartPosition.CenterScreen;

            Rectangle workArea = Screen.PrimaryScreen != null
                ? Screen.PrimaryScreen.WorkingArea
                : new Rectangle(0, 0, 1280, 720);

            int targetW = Math.Min(ThemeTokens.Scale(1080), Math.Max(800, workArea.Width - 40));
            int targetH = Math.Min(ThemeTokens.Scale(780), Math.Max(640, workArea.Height - 40));
            int minW = Math.Min(ThemeTokens.Scale(940), Math.Max(760, workArea.Width - 60));
            int minH = Math.Min(ThemeTokens.Scale(690), Math.Max(560, workArea.Height - 60));

            this.MinimumSize = new Size(minW, minH);
            this.Size = new Size(targetW, targetH);

            // 1. Header Bar
            Panel headerPanel = CreateHeaderPanel();

            // 2. Footer Status Bar
            Panel footerPanel = new Panel();
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.AutoSize = true;
            footerPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            footerPanel.BackColor = ThemeTokens.BgSurface;

            Panel footerBorder = new Panel();
            footerBorder.Dock = DockStyle.Top;
            footerBorder.Height = 1;
            footerBorder.BackColor = ThemeTokens.BorderSubtle;

            _lblFooterStatus = new Label();
            _lblFooterStatus.UseMnemonic = false;
            _lblFooterStatus.Dock = DockStyle.Fill;
            _lblFooterStatus.AutoSize = true;
            _lblFooterStatus.Padding = ThemeTokens.ScaledPadding(16, 6, 16, 6);
            _lblFooterStatus.Font = ThemeTokens.FontCaption;
            _lblFooterStatus.ForeColor = ThemeTokens.TextSecondary;
            _lblFooterStatus.Text = "Siap. Klik 'Aktifkan KeyBinder' untuk mulai menggunakan hotkey secara global.";

            footerPanel.Controls.Add(_lblFooterStatus);
            footerPanel.Controls.Add(footerBorder);

            // 3. Main Workspace Tabs (KeyBinder Manual + Auto Sequence)
            _tabMain = new TabControl();
            _tabMain.Dock = DockStyle.Fill;
            _tabMain.Font = ThemeTokens.FontBody;
            _tabMain.Padding = new Point(ThemeTokens.Scale(14), ThemeTokens.Scale(6));

            TabPage tabManual = new TabPage("KeyBinder (Hotkey Manual)");
            tabManual.BackColor = ThemeTokens.BgApp;

            TableLayoutPanel mainGrid = new TableLayoutPanel();
            mainGrid.Dock = DockStyle.Fill;
            mainGrid.ColumnCount = 2;
            mainGrid.RowCount = 1;
            mainGrid.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);
            mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            mainGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel leftCard = CreateKeybindListCard();
            leftCard.Margin = ThemeTokens.ScaledPadding(0, 0, 8, 0);

            Panel rightColumn = CreateRightColumn();
            rightColumn.Margin = ThemeTokens.ScaledPadding(8, 0, 0, 0);

            mainGrid.Controls.Add(leftCard, 0, 0);
            mainGrid.Controls.Add(rightColumn, 1, 0);
            tabManual.Controls.Add(mainGrid);

            TabPage tabAuto = new TabPage("Auto Sequence (Urutan Otomatis)");
            tabAuto.BackColor = ThemeTokens.BgApp;
            _autoSequenceControl = new AutoSequenceControl(
                _config,
                _windowService,
                _hookService,
                _autoService,
                SaveAndSyncConfig,
                SetFooterStatus);
            tabAuto.Controls.Add(_autoSequenceControl);

            _tabMain.TabPages.Add(tabManual);
            _tabMain.TabPages.Add(tabAuto);

            this.Controls.Add(_tabMain);
            this.Controls.Add(footerPanel);
            this.Controls.Add(headerPanel);
        }

        private Panel CreateHeaderPanel()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.AutoSize = true;
            header.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            header.BackColor = ThemeTokens.BgSurface;

            Panel bottomLine = new Panel();
            bottomLine.Dock = DockStyle.Bottom;
            bottomLine.Height = 1;
            bottomLine.BackColor = ThemeTokens.BorderSubtle;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Padding = ThemeTokens.ScaledPadding(16, 10, 16, 10);
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            TableLayoutPanel titleBox = new TableLayoutPanel();
            titleBox.Dock = DockStyle.Fill;
            titleBox.AutoSize = true;
            titleBox.ColumnCount = 1;
            titleBox.RowCount = 2;
            titleBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            titleBox.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "SafeKeyBinder";
            lblTitle.Font = ThemeTokens.FontTitle;
            lblTitle.ForeColor = ThemeTokens.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0);

            Label lblSub = new Label();
            lblSub.UseMnemonic = false;
            lblSub.Text = "Macro & KeyBinder Universal Windows  •  100% Offline & Open Source";
            lblSub.Font = ThemeTokens.FontCaption;
            lblSub.ForeColor = ThemeTokens.TextSecondary;
            lblSub.AutoSize = true;
            lblSub.Margin = ThemeTokens.ScaledPadding(1, 2, 0, 0);

            titleBox.Controls.Add(lblTitle, 0, 0);
            titleBox.Controls.Add(lblSub, 0, 1);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;
            actions.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            actions.Padding = ThemeTokens.ScaledPadding(0, 4, 0, 0);

            _lblStatusBadge = new Label();
            _lblStatusBadge.UseMnemonic = false;
            _lblStatusBadge.Text = "NONAKTIF";
            _lblStatusBadge.Font = ThemeTokens.FontBodyBold;
            _lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatusBadge.AutoSize = true;
            _lblStatusBadge.MinimumSize = new Size(ThemeTokens.Scale(96), ThemeTokens.Scale(34));
            _lblStatusBadge.Padding = ThemeTokens.ScaledPadding(10, 6, 10, 6);
            _lblStatusBadge.Margin = ThemeTokens.ScaledPadding(0, 0, 8, 0);

            _btnAdminRestart = new Button();
            _btnAdminRestart.Text = IsRunningAsAdmin() ? "Mode Admin" : "Buka sbg Admin";
            ThemeTokens.StyleSecondaryButton(_btnAdminRestart);
            _btnAdminRestart.MinimumSize = new Size(0, ThemeTokens.Scale(34));
            _btnAdminRestart.Enabled = !IsRunningAsAdmin();
            _btnAdminRestart.Margin = ThemeTokens.ScaledPadding(0, 0, 8, 0);

            _btnSecurityInfo = new Button();
            _btnSecurityInfo.Text = "Keamanan & Audit";
            ThemeTokens.StyleSecondaryButton(_btnSecurityInfo);
            _btnSecurityInfo.MinimumSize = new Size(0, ThemeTokens.Scale(34));
            _btnSecurityInfo.Margin = ThemeTokens.ScaledPadding(0, 0, 8, 0);

            _btnStartStop = new Button();
            _btnStartStop.Text = "Aktifkan KeyBinder";
            ThemeTokens.StylePrimaryButton(_btnStartStop);
            _btnStartStop.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
            _btnStartStop.Margin = new Padding(0);

            actions.Controls.Add(_lblStatusBadge);
            actions.Controls.Add(_btnAdminRestart);
            actions.Controls.Add(_btnSecurityInfo);
            actions.Controls.Add(_btnStartStop);

            layout.Controls.Add(titleBox, 0, 0);
            layout.Controls.Add(actions, 1, 0);

            header.Controls.Add(layout);
            header.Controls.Add(bottomLine);
            return header;
        }

        private Panel CreateKeybindListCard()
        {
            Panel card = CreateCardPanel();
            card.Padding = ThemeTokens.ScaledPadding(12, 12, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Header + Action Buttons
            TableLayoutPanel topBar = new TableLayoutPanel();
            topBar.Dock = DockStyle.Fill;
            topBar.AutoSize = true;
            topBar.ColumnCount = 2;
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topBar.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            Label lblListTitle = new Label();
            lblListTitle.UseMnemonic = false;
            lblListTitle.Text = "Daftar Keybind";
            lblListTitle.Font = ThemeTokens.FontSection;
            lblListTitle.ForeColor = ThemeTokens.TextPrimary;
            lblListTitle.AutoSize = true;
            lblListTitle.Anchor = AnchorStyles.Left;

            FlowLayoutPanel toolbar = new FlowLayoutPanel();
            toolbar.AutoSize = true;
            toolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            toolbar.FlowDirection = FlowDirection.LeftToRight;
            toolbar.WrapContents = false;
            toolbar.Anchor = AnchorStyles.Right;

            _btnAddNew = new Button();
            _btnAddNew.Text = "+ Tambah";
            ThemeTokens.StyleSecondaryButton(_btnAddNew);
            _btnAddNew.ForeColor = ThemeTokens.AccentPrimary;
            _btnAddNew.Font = ThemeTokens.FontBodyBold;
            _btnAddNew.Margin = ThemeTokens.ScaledPadding(0, 0, 6, 0);

            _btnDuplicate = new Button();
            _btnDuplicate.Text = "Duplikat";
            ThemeTokens.StyleSecondaryButton(_btnDuplicate);
            _btnDuplicate.Margin = ThemeTokens.ScaledPadding(0, 0, 6, 0);

            _btnDelete = new Button();
            _btnDelete.Text = "Hapus";
            ThemeTokens.StyleSecondaryButton(_btnDelete);
            _btnDelete.ForeColor = ThemeTokens.DangerPrimary;
            _btnDelete.Margin = new Padding(0);

            toolbar.Controls.Add(_btnAddNew);
            toolbar.Controls.Add(_btnDuplicate);
            toolbar.Controls.Add(_btnDelete);

            topBar.Controls.Add(lblListTitle, 0, 0);
            topBar.Controls.Add(toolbar, 1, 0);

            // ListView
            _lvKeybinds = new ListView();
            _lvKeybinds.Dock = DockStyle.Fill;
            _lvKeybinds.View = View.Details;
            _lvKeybinds.FullRowSelect = true;
            _lvKeybinds.GridLines = true;
            _lvKeybinds.CheckBoxes = true;
            _lvKeybinds.MultiSelect = false;
            _lvKeybinds.HideSelection = false;
            _lvKeybinds.BackColor = ThemeTokens.BgSurface;
            _lvKeybinds.Font = ThemeTokens.FontBody;
            EnableDoubleBuffering(_lvKeybinds);

            _lvKeybinds.Columns.Add("Aktif", ThemeTokens.Scale(46));
            _lvKeybinds.Columns.Add("Hotkey", ThemeTokens.Scale(96));
            _lvKeybinds.Columns.Add("Label", ThemeTokens.Scale(112));
            _lvKeybinds.Columns.Add("Teks yang Dikirim", ThemeTokens.Scale(160));
            _lvKeybinds.Columns.Add("Suffix", ThemeTokens.Scale(74));
            _lvKeybinds.SizeChanged += delegate { ResizeListViewColumns(); };

            Label lblHint = new Label();
            lblHint.UseMnemonic = false;
            lblHint.Text = "Centang kotak di kiri baris untuk aktif/nonaktifkan hotkey secara cepat.";
            lblHint.Font = ThemeTokens.FontCaption;
            lblHint.ForeColor = ThemeTokens.TextMuted;
            lblHint.AutoSize = true;
            lblHint.Margin = ThemeTokens.ScaledPadding(0, 8, 0, 0);

            layout.Controls.Add(topBar, 0, 0);
            layout.Controls.Add(_lvKeybinds, 0, 1);
            layout.Controls.Add(lblHint, 0, 2);

            card.Controls.Add(layout);
            return card;
        }

        private Panel CreateRightColumn()
        {
            TableLayoutPanel stack = new TableLayoutPanel();
            stack.Dock = DockStyle.Fill;
            stack.ColumnCount = 1;
            stack.RowCount = 3;
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Panel editorCard = CreateEditorCard();
            editorCard.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            Panel settingsCard = CreateSettingsCard();
            settingsCard.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            Panel testCard = CreateTestAreaCard();

            stack.Controls.Add(editorCard, 0, 0);
            stack.Controls.Add(settingsCard, 0, 1);
            stack.Controls.Add(testCard, 0, 2);

            return stack;
        }

        private Panel CreateEditorCard()
        {
            Panel card = CreateCardPanel();
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 3;
            grid.RowCount = 6;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Label
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Hotkey
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Text
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Prefix/Suffix
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Save/Reset

            _lblEditorMode = new Label();
            _lblEditorMode.UseMnemonic = false;
            _lblEditorMode.Text = "Tambah Keybind Baru";
            _lblEditorMode.Font = ThemeTokens.FontSection;
            _lblEditorMode.ForeColor = ThemeTokens.TextPrimary;
            _lblEditorMode.AutoSize = true;
            _lblEditorMode.Anchor = AnchorStyles.Left;
            _lblEditorMode.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);
            grid.SetColumnSpan(_lblEditorMode, 2);

            _chkEnabled = new CheckBox();
            _chkEnabled.UseMnemonic = false;
            _chkEnabled.Text = "Aktifkan";
            _chkEnabled.Checked = true;
            _chkEnabled.AutoSize = true;
            _chkEnabled.Anchor = AnchorStyles.Right;
            _chkEnabled.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            grid.Controls.Add(_lblEditorMode, 0, 0);
            grid.Controls.Add(_chkEnabled, 2, 0);

            // Label row
            Label lblName = CreateFieldLabel("Label (Opsional)");
            _txtLabel = new TextBox();
            _txtLabel.Dock = DockStyle.Fill;
            _txtLabel.Margin = ThemeTokens.ScaledPadding(8, 3, 0, 4);
            grid.SetColumnSpan(_txtLabel, 2);
            grid.Controls.Add(lblName, 0, 1);
            grid.Controls.Add(_txtLabel, 1, 1);

            // Hotkey row
            Label lblHotkey = CreateFieldLabel("Tombol Hotkey");
            _txtHotkeyPreview = new TextBox();
            _txtHotkeyPreview.ReadOnly = true;
            _txtHotkeyPreview.BackColor = ThemeTokens.BgSubtle;
            _txtHotkeyPreview.Font = ThemeTokens.FontMono;
            _txtHotkeyPreview.Dock = DockStyle.Fill;
            _txtHotkeyPreview.Margin = ThemeTokens.ScaledPadding(8, 4, 8, 4);

            _btnRecordHotkey = new Button();
            _btnRecordHotkey.Text = "Rekam Tombol...";
            ThemeTokens.StyleSecondaryButton(_btnRecordHotkey);
            _btnRecordHotkey.Anchor = AnchorStyles.Right;
            _btnRecordHotkey.Margin = ThemeTokens.ScaledPadding(0, 2, 0, 2);

            grid.Controls.Add(lblHotkey, 0, 2);
            grid.Controls.Add(_txtHotkeyPreview, 1, 2);
            grid.Controls.Add(_btnRecordHotkey, 2, 2);

            // Text row
            Label lblText = CreateFieldLabel("Teks / Isi Macro");
            lblText.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            lblText.Margin = ThemeTokens.ScaledPadding(0, 6, 0, 0);

            _txtTextToSend = new TextBox();
            _txtTextToSend.Multiline = true;
            _txtTextToSend.AcceptsReturn = true;
            _txtTextToSend.ScrollBars = ScrollBars.Vertical;
            _txtTextToSend.Dock = DockStyle.Fill;
            _txtTextToSend.MinimumSize = new Size(0, ThemeTokens.Scale(48));
            _txtTextToSend.Margin = ThemeTokens.ScaledPadding(8, 3, 0, 3);
            grid.SetColumnSpan(_txtTextToSend, 2);

            grid.Controls.Add(lblText, 0, 3);
            grid.Controls.Add(_txtTextToSend, 1, 3);

            // Prefix & Suffix row
            Label lblExtra = CreateFieldLabel("Prefix & Suffix");

            TableLayoutPanel extraPanel = new TableLayoutPanel();
            extraPanel.Dock = DockStyle.Fill;
            extraPanel.AutoSize = true;
            extraPanel.ColumnCount = 4;
            extraPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            extraPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            extraPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            extraPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            extraPanel.Margin = ThemeTokens.ScaledPadding(8, 2, 0, 2);
            grid.SetColumnSpan(extraPanel, 2);

            _txtPrefixPreview = new TextBox();
            _txtPrefixPreview.ReadOnly = true;
            _txtPrefixPreview.BackColor = ThemeTokens.BgSubtle;
            _txtPrefixPreview.Dock = DockStyle.Fill;
            _txtPrefixPreview.Margin = ThemeTokens.ScaledPadding(0, 4, 6, 4);

            _btnRecordPrefix = new Button();
            _btnRecordPrefix.Text = "Prefix...";
            ThemeTokens.StyleSecondaryButton(_btnRecordPrefix);
            _btnRecordPrefix.Anchor = AnchorStyles.Left;
            _btnRecordPrefix.Margin = ThemeTokens.ScaledPadding(0, 2, 10, 2);

            Label lblSuffix = new Label();
            lblSuffix.UseMnemonic = false;
            lblSuffix.Text = "Suffix:";
            lblSuffix.AutoSize = true;
            lblSuffix.Anchor = AnchorStyles.Right;
            lblSuffix.ForeColor = ThemeTokens.TextSecondary;
            lblSuffix.Margin = ThemeTokens.ScaledPadding(0, 0, 6, 0);

            _cmbSuffix = new ComboBox();
            _cmbSuffix.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbSuffix.Dock = DockStyle.Fill;
            _cmbSuffix.Margin = ThemeTokens.ScaledPadding(0, 3, 0, 3);
            _cmbSuffix.Items.Add("Tidak ada");
            _cmbSuffix.Items.Add("Enter");
            _cmbSuffix.Items.Add("Tab");
            _cmbSuffix.Items.Add("Space");
            _cmbSuffix.SelectedIndex = 0;

            extraPanel.Controls.Add(_txtPrefixPreview, 0, 0);
            extraPanel.Controls.Add(_btnRecordPrefix, 1, 0);
            extraPanel.Controls.Add(lblSuffix, 2, 0);
            extraPanel.Controls.Add(_cmbSuffix, 3, 0);

            grid.Controls.Add(lblExtra, 0, 4);
            grid.Controls.Add(extraPanel, 1, 4);

            // Action row (HIG consistent: Cancel/Reset on left, Save on right)
            TableLayoutPanel saveBar = new TableLayoutPanel();
            saveBar.Dock = DockStyle.Fill;
            saveBar.AutoSize = true;
            saveBar.ColumnCount = 2;
            saveBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            saveBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            saveBar.Margin = ThemeTokens.ScaledPadding(0, 8, 0, 0);
            grid.SetColumnSpan(saveBar, 3);

            _btnResetEditor = new Button();
            _btnResetEditor.Text = "Batal / Baru";
            ThemeTokens.StyleSecondaryButton(_btnResetEditor);
            _btnResetEditor.MinimumSize = new Size(0, ThemeTokens.Scale(32));
            _btnResetEditor.Anchor = AnchorStyles.Left;

            _btnSaveItem = new Button();
            _btnSaveItem.Text = "Simpan Keybind";
            ThemeTokens.StylePrimaryButton(_btnSaveItem);
            _btnSaveItem.Anchor = AnchorStyles.Right;

            saveBar.Controls.Add(_btnResetEditor, 0, 0);
            saveBar.Controls.Add(_btnSaveItem, 1, 0);

            grid.Controls.Add(saveBar, 0, 5);

            card.Controls.Add(grid);
            return card;
        }

        private Panel CreateSettingsCard()
        {
            Panel card = CreateCardPanel();
            card.AutoSize = true;
            card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Top;
            grid.AutoSize = true;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.ColumnCount = 3;
            grid.RowCount = 5;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "Pengaturan Universal & Kontrol";
            lblTitle.Font = ThemeTokens.FontSection;
            lblTitle.ForeColor = ThemeTokens.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);
            grid.SetColumnSpan(lblTitle, 3);
            grid.Controls.Add(lblTitle, 0, 0);

            // Scope Radios
            Label lblScope = CreateFieldLabel("Target Aplikasi");
            FlowLayoutPanel radioFlow = new FlowLayoutPanel();
            radioFlow.AutoSize = true;
            radioFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            radioFlow.WrapContents = true;
            radioFlow.Margin = ThemeTokens.ScaledPadding(8, 0, 0, 4);
            grid.SetColumnSpan(radioFlow, 2);

            _rbScopeGlobal = new RadioButton();
            _rbScopeGlobal.UseMnemonic = false;
            _rbScopeGlobal.Text = "Semua Aplikasi (Global)";
            _rbScopeGlobal.AutoSize = true;
            _rbScopeGlobal.Checked = true;
            _rbScopeGlobal.Margin = ThemeTokens.ScaledPadding(0, 2, 14, 2);

            _rbScopeWindow = new RadioButton();
            _rbScopeWindow.UseMnemonic = false;
            _rbScopeWindow.Text = "Hanya Aplikasi Tertentu";
            _rbScopeWindow.AutoSize = true;
            _rbScopeWindow.Margin = ThemeTokens.ScaledPadding(0, 2, 0, 2);

            radioFlow.Controls.Add(_rbScopeGlobal);
            radioFlow.Controls.Add(_rbScopeWindow);

            grid.Controls.Add(lblScope, 0, 1);
            grid.Controls.Add(radioFlow, 1, 1);

            // Window Filter
            Label lblWindow = CreateFieldLabel("Filter Window");
            _cmbWindowTarget = new ComboBox();
            _cmbWindowTarget.DropDownStyle = ComboBoxStyle.DropDown;
            _cmbWindowTarget.Dock = DockStyle.Fill;
            _cmbWindowTarget.Margin = ThemeTokens.ScaledPadding(8, 3, 8, 4);

            _btnRefreshWindows = new Button();
            _btnRefreshWindows.Text = "Refresh";
            ThemeTokens.StyleSecondaryButton(_btnRefreshWindows);
            _btnRefreshWindows.Anchor = AnchorStyles.Right;
            _btnRefreshWindows.Margin = ThemeTokens.ScaledPadding(0, 1, 0, 2);

            grid.Controls.Add(lblWindow, 0, 2);
            grid.Controls.Add(_cmbWindowTarget, 1, 2);
            grid.Controls.Add(_btnRefreshWindows, 2, 2);

            // Master Toggle
            Label lblMaster = CreateFieldLabel("Master Toggle Key");
            _txtMasterToggle = new TextBox();
            _txtMasterToggle.ReadOnly = true;
            _txtMasterToggle.BackColor = ThemeTokens.BgSubtle;
            _txtMasterToggle.Font = ThemeTokens.FontMono;
            _txtMasterToggle.Dock = DockStyle.Fill;
            _txtMasterToggle.Margin = ThemeTokens.ScaledPadding(8, 3, 8, 4);

            _btnRecordMaster = new Button();
            _btnRecordMaster.Text = "Ubah...";
            ThemeTokens.StyleSecondaryButton(_btnRecordMaster);
            _btnRecordMaster.Anchor = AnchorStyles.Right;
            _btnRecordMaster.Margin = ThemeTokens.ScaledPadding(0, 1, 0, 2);

            grid.Controls.Add(lblMaster, 0, 3);
            grid.Controls.Add(_txtMasterToggle, 1, 3);
            grid.Controls.Add(_btnRecordMaster, 2, 3);

            // Optional Typing Pause + Typing Delay
            _chkTypingPause = new CheckBox();
            _chkTypingPause.UseMnemonic = false;
            _chkTypingPause.Text = "Pause saat ketik:";
            _chkTypingPause.AutoSize = true;
            _chkTypingPause.Anchor = AnchorStyles.Left;
            _chkTypingPause.Margin = ThemeTokens.ScaledPadding(0, 4, 0, 2);

            TableLayoutPanel pauseDelayPanel = new TableLayoutPanel();
            pauseDelayPanel.Dock = DockStyle.Fill;
            pauseDelayPanel.AutoSize = true;
            pauseDelayPanel.ColumnCount = 4;
            pauseDelayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            pauseDelayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pauseDelayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pauseDelayPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemeTokens.Scale(72)));
            pauseDelayPanel.Margin = ThemeTokens.ScaledPadding(8, 2, 0, 0);
            grid.SetColumnSpan(pauseDelayPanel, 2);

            _txtTypingPauseKey = new TextBox();
            _txtTypingPauseKey.ReadOnly = true;
            _txtTypingPauseKey.BackColor = ThemeTokens.BgSubtle;
            _txtTypingPauseKey.Dock = DockStyle.Fill;
            _txtTypingPauseKey.Margin = ThemeTokens.ScaledPadding(0, 4, 6, 2);

            _btnRecordTypingPause = new Button();
            _btnRecordTypingPause.Text = "Pilih...";
            ThemeTokens.StyleSecondaryButton(_btnRecordTypingPause);
            _btnRecordTypingPause.Anchor = AnchorStyles.Left;
            _btnRecordTypingPause.Margin = ThemeTokens.ScaledPadding(0, 2, 12, 2);

            Label lblDelay = new Label();
            lblDelay.UseMnemonic = false;
            lblDelay.Text = "Delay (ms):";
            lblDelay.AutoSize = true;
            lblDelay.ForeColor = ThemeTokens.TextSecondary;
            lblDelay.Anchor = AnchorStyles.Right;
            lblDelay.Margin = ThemeTokens.ScaledPadding(0, 0, 6, 0);

            _numTypingDelay = new NumericUpDown();
            _numTypingDelay.Minimum = 0;
            _numTypingDelay.Maximum = 100;
            _numTypingDelay.Dock = DockStyle.Fill;
            _numTypingDelay.Margin = ThemeTokens.ScaledPadding(0, 4, 0, 2);

            pauseDelayPanel.Controls.Add(_txtTypingPauseKey, 0, 0);
            pauseDelayPanel.Controls.Add(_btnRecordTypingPause, 1, 0);
            pauseDelayPanel.Controls.Add(lblDelay, 2, 0);
            pauseDelayPanel.Controls.Add(_numTypingDelay, 3, 0);

            grid.Controls.Add(_chkTypingPause, 0, 4);
            grid.Controls.Add(pauseDelayPanel, 1, 4);

            card.Controls.Add(grid);
            return card;
        }

        private Panel CreateTestAreaCard()
        {
            Panel card = CreateCardPanel();
            card.AutoSize = true;
            card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ThemeTokens.Scale(56)));

            Label lblTest = new Label();
            lblTest.UseMnemonic = false;
            lblTest.Text = "Kotak Uji Coba (Aktifkan lalu klik di bawah & tekan hotkey)";
            lblTest.Font = ThemeTokens.FontCaption;
            lblTest.ForeColor = ThemeTokens.TextSecondary;
            lblTest.AutoSize = true;
            lblTest.Anchor = AnchorStyles.Left;
            lblTest.Margin = ThemeTokens.ScaledPadding(0, 0, 8, 6);

            Button btnClearTest = new Button();
            btnClearTest.Text = "Bersihkan";
            ThemeTokens.StyleSecondaryButton(btnClearTest);
            btnClearTest.MinimumSize = new Size(0, ThemeTokens.Scale(26));
            btnClearTest.Padding = ThemeTokens.ScaledPadding(10, 1, 10, 1);
            btnClearTest.Anchor = AnchorStyles.Right;
            btnClearTest.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);
            btnClearTest.Click += delegate { _txtTestArea.Clear(); };

            _txtTestArea = new TextBox();
            _txtTestArea.Multiline = true;
            _txtTestArea.ScrollBars = ScrollBars.Vertical;
            _txtTestArea.Dock = DockStyle.Fill;
            _txtTestArea.BackColor = ThemeTokens.BgSubtle;
            _txtTestArea.Font = ThemeTokens.FontBody;
            layout.SetColumnSpan(_txtTestArea, 2);

            layout.Controls.Add(lblTest, 0, 0);
            layout.Controls.Add(btnClearTest, 1, 0);
            layout.Controls.Add(_txtTestArea, 0, 1);

            card.Controls.Add(layout);
            return card;
        }

        private void ResizeListViewColumns()
        {
            if (_lvKeybinds == null || _lvKeybinds.Columns.Count < 5) return;
            int fixedWidth =
                _lvKeybinds.Columns[0].Width +
                _lvKeybinds.Columns[1].Width +
                _lvKeybinds.Columns[2].Width +
                _lvKeybinds.Columns[4].Width +
                ThemeTokens.Scale(8);

            int remaining = _lvKeybinds.ClientSize.Width - fixedWidth;
            if (remaining > ThemeTokens.Scale(110))
            {
                _lvKeybinds.Columns[3].Width = remaining;
            }
        }

        private void WireEvents()
        {
            _btnStartStop.Click += OnToggleStartStopClick;
            _btnSecurityInfo.Click += OnSecurityInfoClick;
            _btnAdminRestart.Click += OnRestartAsAdminClick;

            _lvKeybinds.SelectedIndexChanged += OnListSelectionChanged;
            this.Shown += delegate
            {
                _lvKeybinds.ItemChecked += OnListItemChecked;
            };

            _btnAddNew.Click += delegate { PrepareNewItemEditor(); };
            _btnDuplicate.Click += OnDuplicateSelectedClick;
            _btnDelete.Click += OnDeleteSelectedClick;

            _btnRecordHotkey.Click += OnRecordHotkeyClick;
            _btnRecordPrefix.Click += OnRecordPrefixClick;
            _btnResetEditor.Click += delegate { PrepareNewItemEditor(); };
            _btnSaveItem.Click += OnSaveEditorClick;

            _rbScopeGlobal.CheckedChanged += OnSettingsChanged;
            _rbScopeWindow.CheckedChanged += OnSettingsChanged;
            _cmbWindowTarget.TextChanged += OnSettingsChanged;
            _btnRefreshWindows.Click += delegate { RefreshOpenWindowsList(); };

            _btnRecordMaster.Click += OnRecordMasterToggleClick;
            _chkTypingPause.CheckedChanged += OnSettingsChanged;
            _btnRecordTypingPause.Click += OnRecordTypingPauseClick;
            _numTypingDelay.ValueChanged += OnSettingsChanged;

            _hookService.StatusChanged += OnHookStatusChanged;
            _hookService.KeybindTriggered += OnHookKeybindTriggered;

            this.FormClosing += delegate
            {
                _autoService.Stop();
                _hookService.ShutdownAll();
                SaveConfigSafe();
            };
        }

        private void OnToggleStartStopClick(object sender, EventArgs e)
        {
            if (_hookService.IsManualActive)
            {
                _hookService.Stop();
                SetFooterStatus("KeyBinder manual dimatikan.");
            }
            else
            {
                try
                {
                    _hookService.Start(_config);
                    SetFooterStatus("KeyBinder AKTIF. Tekan " +
                        KeybindItem.FormatHotkey(
                            _config.MasterToggleVirtualKey,
                            _config.MasterToggleCtrl,
                            _config.MasterToggleAlt,
                            _config.MasterToggleShift) +
                        " kapan saja untuk Pause/Resume.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Tidak dapat mengaktifkan global keyboard hook:\n" + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void OnSecurityInfoClick(object sender, EventArgs e)
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath);
            string srcDir = Path.Combine(exeDir, "src");
            using (SecurityAuditDialog dlg = new SecurityAuditDialog(srcDir, _configService.ConfigFilePath))
            {
                dlg.ShowDialog(this);
            }
        }

        private void OnRestartAsAdminClick(object sender, EventArgs e)
        {
            try
            {
                SaveConfigSafe();
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Application.ExecutablePath;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                Application.Exit();
            }
            catch
            {
                SetFooterStatus("Restart sebagai Administrator dibatalkan.");
            }
        }

        private void OnListSelectionChanged(object sender, EventArgs e)
        {
            if (_lvKeybinds.SelectedItems.Count == 0) return;
            KeybindItem item = _lvKeybinds.SelectedItems[0].Tag as KeybindItem;
            if (item != null)
            {
                LoadItemIntoEditor(item);
            }
        }

        private void OnListItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_isPopulatingList) return;
            KeybindItem item = e.Item.Tag as KeybindItem;
            if (item != null && item.Enabled != e.Item.Checked)
            {
                item.Enabled = e.Item.Checked;
                if (_editingItemId == item.Id)
                {
                    _chkEnabled.Checked = item.Enabled;
                }
                SaveAndSyncConfig();
                SetFooterStatus("Keybind '" + GetDisplayLabel(item) + "' " + (item.Enabled ? "diaktifkan." : "dinonaktifkan."));
            }
        }

        private void OnDuplicateSelectedClick(object sender, EventArgs e)
        {
            if (_lvKeybinds.SelectedItems.Count == 0)
            {
                MessageBox.Show("Pilih keybind di daftar yang ingin diduplikat.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            KeybindItem source = _lvKeybinds.SelectedItems[0].Tag as KeybindItem;
            if (source == null) return;

            KeybindItem copy = source.Clone();
            copy.Id = Guid.NewGuid().ToString("N");
            copy.Label = string.IsNullOrEmpty(copy.Label) ? "Salinan" : copy.Label + " (Salinan)";
            copy.VirtualKey = 0;
            copy.Enabled = false;

            _config.Keybinds.Add(copy);
            SaveAndSyncConfig();
            PopulateKeybindList();
            SelectListItemById(copy.Id);
            LoadItemIntoEditor(copy);
            SetFooterStatus("Keybind diduplikat. Silakan rekam tombol hotkey baru lalu simpan.");
        }

        private void OnDeleteSelectedClick(object sender, EventArgs e)
        {
            if (_lvKeybinds.SelectedItems.Count == 0)
            {
                MessageBox.Show("Pilih keybind yang ingin dihapus.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            KeybindItem item = _lvKeybinds.SelectedItems[0].Tag as KeybindItem;
            if (item == null) return;

            DialogResult confirm = MessageBox.Show(
                "Hapus keybind '" + GetDisplayLabel(item) + "'?",
                "Konfirmasi Hapus",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _config.Keybinds.Remove(item);
                SaveAndSyncConfig();
                PopulateKeybindList();
                PrepareNewItemEditor();
                SetFooterStatus("Keybind dihapus.");
            }
        }

        private void OnRecordHotkeyClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Rekam Hotkey",
                    _editorVirtualKey,
                    _editorCtrl,
                    _editorAlt,
                    _editorShift,
                    true,
                    false))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _editorVirtualKey = dlg.SelectedVirtualKey;
                        _editorCtrl = dlg.SelectedCtrl;
                        _editorAlt = dlg.SelectedAlt;
                        _editorShift = dlg.SelectedShift;
                        _txtHotkeyPreview.Text = KeybindItem.FormatHotkey(_editorVirtualKey, _editorCtrl, _editorAlt, _editorShift);
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnRecordPrefixClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Pilih Prefix Key (Opsional)",
                    _editorPrefixVk,
                    false,
                    false,
                    false,
                    false,
                    true))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _editorPrefixVk = dlg.SelectedVirtualKey;
                        _txtPrefixPreview.Text = _editorPrefixVk > 0
                            ? KeybindItem.FormatKeyName(_editorPrefixVk)
                            : "(Tidak ada)";
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnSaveEditorClick(object sender, EventArgs e)
        {
            if (_editorVirtualKey <= 0)
            {
                MessageBox.Show("Silakan rekam tombol Hotkey terlebih dahulu.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_txtTextToSend.Text) &&
                _editorPrefixVk <= 0 &&
                _cmbSuffix.SelectedIndex <= 0)
            {
                MessageBox.Show("Isi teks yang ingin dikirim atau pilih Prefix/Suffix.", "Validasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int i = 0; i < _config.Keybinds.Count; i++)
            {
                KeybindItem other = _config.Keybinds[i];
                if (other.Id != _editingItemId &&
                    other.VirtualKey == _editorVirtualKey &&
                    other.RequireCtrl == _editorCtrl &&
                    other.RequireAlt == _editorAlt &&
                    other.RequireShift == _editorShift)
                {
                    MessageBox.Show(
                        "Kombinasi tombol " + other.GetHotkeyDisplay() + " sudah dipakai oleh keybind '" + GetDisplayLabel(other) + "'.",
                        "Konflik Hotkey",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            KeybindItem target = null;
            if (!string.IsNullOrEmpty(_editingItemId))
            {
                target = _config.Keybinds.Find(delegate(KeybindItem x) { return x.Id == _editingItemId; });
            }

            bool isNew = (target == null);
            if (isNew)
            {
                target = new KeybindItem();
                _config.Keybinds.Add(target);
            }

            target.Label = (_txtLabel.Text ?? string.Empty).Trim();
            target.VirtualKey = _editorVirtualKey;
            target.RequireCtrl = _editorCtrl;
            target.RequireAlt = _editorAlt;
            target.RequireShift = _editorShift;
            target.TextToSend = _txtTextToSend.Text ?? string.Empty;
            target.PrefixVirtualKey = _editorPrefixVk;
            target.Suffix = (SuffixAction)_cmbSuffix.SelectedIndex;
            target.Enabled = _chkEnabled.Checked;

            SaveAndSyncConfig();
            PopulateKeybindList();
            SelectListItemById(target.Id);
            LoadItemIntoEditor(target);

            SetFooterStatus((isNew ? "Keybind baru ditambahkan: " : "Keybind diperbarui: ") + target.GetHotkeyDisplay());
        }

        private void OnRecordMasterToggleClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Atur Master Toggle Hotkey",
                    _config.MasterToggleVirtualKey,
                    _config.MasterToggleCtrl,
                    _config.MasterToggleAlt,
                    _config.MasterToggleShift,
                    true,
                    false))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedVirtualKey > 0)
                    {
                        _config.MasterToggleVirtualKey = dlg.SelectedVirtualKey;
                        _config.MasterToggleCtrl = dlg.SelectedCtrl;
                        _config.MasterToggleAlt = dlg.SelectedAlt;
                        _config.MasterToggleShift = dlg.SelectedShift;
                        _txtMasterToggle.Text = KeybindItem.FormatHotkey(
                            _config.MasterToggleVirtualKey,
                            _config.MasterToggleCtrl,
                            _config.MasterToggleAlt,
                            _config.MasterToggleShift);
                        SaveAndSyncConfig();
                        SetFooterStatus("Master Toggle Hotkey diperbarui ke " + _txtMasterToggle.Text);
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnRecordTypingPauseClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Pilih Tombol Pause Saat Ketik",
                    _config.TypingPauseVirtualKey,
                    false,
                    false,
                    false,
                    false,
                    false))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedVirtualKey > 0)
                    {
                        _config.TypingPauseVirtualKey = dlg.SelectedVirtualKey;
                        _txtTypingPauseKey.Text = KeybindItem.FormatKeyName(_config.TypingPauseVirtualKey);
                        SaveAndSyncConfig();
                        SetFooterStatus("Tombol Pause Saat Ketik diatur ke " + _txtTypingPauseKey.Text + " (Resume: Enter/Esc).");
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings) return;

            _config.TargetScope = _rbScopeWindow.Checked
                ? TargetScopeMode.WindowFilter
                : TargetScopeMode.Global;
            _config.WindowFilterKeyword = _cmbWindowTarget.Text ?? string.Empty;
            _config.TypingPauseEnabled = _chkTypingPause.Checked;
            _config.TypingDelayMs = (int)_numTypingDelay.Value;

            UpdateSettingsControlAvailability();
            SaveAndSyncConfig();
        }

        private void OnHookStatusChanged(BinderStatus status)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<BinderStatus>(UpdateStatusUi), status);
            }
            else
            {
                UpdateStatusUi(status);
            }
        }

        private void OnHookKeybindTriggered(KeybindItem item)
        {
            if (this.IsDisposed) return;
            string msg = "Terkirim [" + item.GetHotkeyDisplay() + "] — " + GetDisplayLabel(item);
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(SetFooterStatus), msg);
            }
            else
            {
                SetFooterStatus(msg);
            }
        }

        private void UpdateStatusUi(BinderStatus status)
        {
            switch (status)
            {
                case BinderStatus.Active:
                    _lblStatusBadge.Text = "AKTIF";
                    _lblStatusBadge.BackColor = ThemeTokens.SuccessBg;
                    _lblStatusBadge.ForeColor = ThemeTokens.SuccessText;
                    _btnStartStop.Text = "Matikan KeyBinder";
                    ThemeTokens.StyleDangerButton(_btnStartStop);
                    break;
                case BinderStatus.PausedByMaster:
                    _lblStatusBadge.Text = "PAUSE (MASTER)";
                    _lblStatusBadge.BackColor = ThemeTokens.WarningBg;
                    _lblStatusBadge.ForeColor = ThemeTokens.WarningText;
                    _btnStartStop.Text = "Matikan KeyBinder";
                    ThemeTokens.StyleDangerButton(_btnStartStop);
                    break;
                case BinderStatus.PausedByTyping:
                    _lblStatusBadge.Text = "PAUSE (KETIK)";
                    _lblStatusBadge.BackColor = ThemeTokens.WarningBg;
                    _lblStatusBadge.ForeColor = ThemeTokens.WarningText;
                    _btnStartStop.Text = "Matikan KeyBinder";
                    ThemeTokens.StyleDangerButton(_btnStartStop);
                    break;
                default:
                    _lblStatusBadge.Text = "NONAKTIF";
                    _lblStatusBadge.BackColor = ThemeTokens.NeutralBadgeBg;
                    _lblStatusBadge.ForeColor = ThemeTokens.TextSecondary;
                    _btnStartStop.Text = "Aktifkan KeyBinder";
                    ThemeTokens.StylePrimaryButton(_btnStartStop);
                    break;
            }
            _btnStartStop.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
        }

        private void LoadSettingsIntoUi()
        {
            _isLoadingSettings = true;
            try
            {
                _rbScopeGlobal.Checked = (_config.TargetScope == TargetScopeMode.Global);
                _rbScopeWindow.Checked = (_config.TargetScope == TargetScopeMode.WindowFilter);
                _cmbWindowTarget.Text = _config.WindowFilterKeyword ?? string.Empty;
                _txtMasterToggle.Text = KeybindItem.FormatHotkey(
                    _config.MasterToggleVirtualKey,
                    _config.MasterToggleCtrl,
                    _config.MasterToggleAlt,
                    _config.MasterToggleShift);
                _chkTypingPause.Checked = _config.TypingPauseEnabled;
                _txtTypingPauseKey.Text = KeybindItem.FormatKeyName(_config.TypingPauseVirtualKey);

                int delay = _config.TypingDelayMs;
                if (delay < (int)_numTypingDelay.Minimum) delay = (int)_numTypingDelay.Minimum;
                if (delay > (int)_numTypingDelay.Maximum) delay = (int)_numTypingDelay.Maximum;
                _numTypingDelay.Value = delay;

                UpdateSettingsControlAvailability();
            }
            finally
            {
                _isLoadingSettings = false;
            }
        }

        private void UpdateSettingsControlAvailability()
        {
            bool windowMode = _rbScopeWindow.Checked;
            _cmbWindowTarget.Enabled = windowMode;
            _btnRefreshWindows.Enabled = windowMode;

            bool typingPause = _chkTypingPause.Checked;
            _txtTypingPauseKey.Enabled = typingPause;
            _btnRecordTypingPause.Enabled = typingPause;
        }

        private void RefreshOpenWindowsList()
        {
            string currentText = _cmbWindowTarget.Text;
            _cmbWindowTarget.Items.Clear();

            int currentPid = Process.GetCurrentProcess().Id;
            List<WindowInfo> windows = _windowService.GetOpenWindows(currentPid);
            for (int i = 0; i < windows.Count; i++)
            {
                _cmbWindowTarget.Items.Add(windows[i].DisplayLabel);
            }

            _cmbWindowTarget.Text = currentText;
        }

        private void PopulateKeybindList()
        {
            _isPopulatingList = true;
            _lvKeybinds.BeginUpdate();
            try
            {
                _lvKeybinds.Items.Clear();
                for (int i = 0; i < _config.Keybinds.Count; i++)
                {
                    KeybindItem item = _config.Keybinds[i];
                    ListViewItem row = new ListViewItem(string.Empty);
                    row.Checked = item.Enabled;
                    row.SubItems.Add(item.GetHotkeyDisplay());
                    row.SubItems.Add(string.IsNullOrEmpty(item.Label) ? "-" : item.Label);
                    row.SubItems.Add(FormatSingleLinePreview(item.TextToSend));
                    row.SubItems.Add(item.GetSuffixDisplay());
                    row.Tag = item;
                    _lvKeybinds.Items.Add(row);
                }
                ResizeListViewColumns();
            }
            finally
            {
                _lvKeybinds.EndUpdate();
                _isPopulatingList = false;
            }
        }

        private void SelectListItemById(string id)
        {
            for (int i = 0; i < _lvKeybinds.Items.Count; i++)
            {
                KeybindItem item = _lvKeybinds.Items[i].Tag as KeybindItem;
                if (item != null && item.Id == id)
                {
                    _lvKeybinds.Items[i].Selected = true;
                    _lvKeybinds.Items[i].EnsureVisible();
                    break;
                }
            }
        }

        private void PrepareNewItemEditor()
        {
            _editingItemId = null;
            _lblEditorMode.Text = "Tambah Keybind Baru";
            _txtLabel.Text = string.Empty;
            _editorVirtualKey = 0;
            _editorCtrl = false;
            _editorAlt = false;
            _editorShift = false;
            _txtHotkeyPreview.Text = "(Klik 'Rekam Tombol...')";
            _txtTextToSend.Text = string.Empty;
            _editorPrefixVk = 0;
            _txtPrefixPreview.Text = "(Tidak ada)";
            _cmbSuffix.SelectedIndex = 0;
            _chkEnabled.Checked = true;
            _btnSaveItem.Text = "Tambah Keybind";
            _lvKeybinds.SelectedItems.Clear();
        }

        private void LoadItemIntoEditor(KeybindItem item)
        {
            _editingItemId = item.Id;
            _lblEditorMode.Text = "Edit Keybind";
            _txtLabel.Text = item.Label ?? string.Empty;
            _editorVirtualKey = item.VirtualKey;
            _editorCtrl = item.RequireCtrl;
            _editorAlt = item.RequireAlt;
            _editorShift = item.RequireShift;
            _txtHotkeyPreview.Text = item.GetHotkeyDisplay();
            _txtTextToSend.Text = item.TextToSend ?? string.Empty;
            _editorPrefixVk = item.PrefixVirtualKey;
            _txtPrefixPreview.Text = _editorPrefixVk > 0
                ? KeybindItem.FormatKeyName(_editorPrefixVk)
                : "(Tidak ada)";
            _cmbSuffix.SelectedIndex = (int)item.Suffix;
            _chkEnabled.Checked = item.Enabled;
            _btnSaveItem.Text = "Simpan Perubahan";
        }

        private void SaveAndSyncConfig()
        {
            _hookService.SetConfig(_config);
            SaveConfigSafe();
        }

        private void SaveConfigSafe()
        {
            try
            {
                _configService.Save(_config);
            }
            catch (Exception ex)
            {
                SetFooterStatus("Gagal menyimpan konfigurasi: " + ex.Message);
            }
        }

        private void SetFooterStatus(string text)
        {
            _lblFooterStatus.Text = text;
        }

        private static string FormatSingleLinePreview(string text)
        {
            if (string.IsNullOrEmpty(text)) return "(Kosong)";
            string clean = text.Replace("\r\n", " ↵ ").Replace("\n", " ↵ ");
            if (clean.Length > 48)
            {
                return clean.Substring(0, 45) + "...";
            }
            return clean;
        }

        private static string GetDisplayLabel(KeybindItem item)
        {
            if (!string.IsNullOrEmpty(item.Label)) return item.Label;
            return item.GetHotkeyDisplay();
        }

        private static Panel CreateCardPanel()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = ThemeTokens.BgSurface;
            p.Paint += delegate(object s, PaintEventArgs e)
            {
                ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle, ThemeTokens.BorderSubtle, ButtonBorderStyle.Solid);
            };
            return p;
        }

        private static Label CreateFieldLabel(string text)
        {
            Label lbl = new Label();
            lbl.UseMnemonic = false;
            lbl.Text = text;
            lbl.Font = ThemeTokens.FontBody;
            lbl.ForeColor = ThemeTokens.TextSecondary;
            lbl.AutoSize = true;
            lbl.Anchor = AnchorStyles.Left;
            lbl.Margin = ThemeTokens.ScaledPadding(0, 4, 0, 4);
            return lbl;
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void EnableDoubleBuffering(Control ctrl)
        {
            PropertyInfo prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null)
            {
                prop.SetValue(ctrl, true, null);
            }
        }
    }
}
