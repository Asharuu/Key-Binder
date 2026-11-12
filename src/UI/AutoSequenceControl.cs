using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SafeKeyBinder.Models;
using SafeKeyBinder.Services;

namespace SafeKeyBinder.UI
{
    public class AutoSequenceControl : UserControl
    {
        private readonly AppConfig _config;
        private readonly WindowService _windowService;
        private readonly KeyboardHookService _hookService;
        private readonly AutoSequenceService _autoService;
        private readonly Action _saveConfigCallback;
        private readonly Action<string> _footerCallback;

        private List<string> _currentItems = new List<string>();
        private int _startIndex = 0;
        private bool _isLoading = false;
        private bool _isUpdatingQueueSelection = false;

        // Left Panel - Pattern Generator
        private RadioButton _rbModeLetter;
        private RadioButton _rbModeNumber;
        private RadioButton _rbModeCustom;

        private Panel _pnlLetterInputs;
        private TextBox _txtLetterPrefix;
        private TextBox _txtLetterSuffix;
        private TextBox _txtLetterStart;
        private TextBox _txtLetterEnd;

        private Panel _pnlNumberInputs;
        private TextBox _txtNumPrefix;
        private TextBox _txtNumSuffix;
        private NumericUpDown _numStart;
        private NumericUpDown _numEnd;
        private NumericUpDown _numPad;

        private Panel _pnlCustomInputs;
        private TextBox _txtCustomLines;

        private Button _btnGenerate;

        // Left Panel - Queue
        private Label _lblQueueInfo;
        private ListBox _lstQueue;
        private Button _btnResetIndex;
        private Button _btnCopyToManual;

        // Right Panel - Settings
        private TextBox _txtTemplate;
        private TextBox _txtPrefixKey;
        private Button _btnPickPrefix;
        private NumericUpDown _numPrefixDelay;
        private ComboBox _cmbSuffix;
        private NumericUpDown _numTypingDelay;
        private NumericUpDown _numIntervalMs;
        private Label _lblIntervalSeconds;
        private NumericUpDown _numCountdownSec;
        private CheckBox _chkLoop;
        private CheckBox _chkRequireWindow;
        private ComboBox _cmbTargetWindow;
        private Button _btnRefreshWindows;

        // Right Panel - Hotkeys & Controls
        private TextBox _txtPauseHotkey;
        private Button _btnPickPauseHotkey;
        private TextBox _txtStopHotkey;
        private Button _btnPickStopHotkey;
        private Label _lblStepPreview;
        private Label _lblRunStatus;
        private Button _btnStopAuto;
        private Button _btnStartPauseAuto;

        public AutoSequenceControl(
            AppConfig config,
            WindowService windowService,
            KeyboardHookService hookService,
            AutoSequenceService autoService,
            Action saveConfigCallback,
            Action<string> footerCallback)
        {
            _config = config;
            _windowService = windowService;
            _hookService = hookService;
            _autoService = autoService;
            _saveConfigCallback = saveConfigCallback;
            _footerCallback = footerCallback;

            if (_config.AutoSequence == null)
            {
                _config.AutoSequence = new AutoSequenceConfig();
            }

            this.Dock = DockStyle.Fill;
            this.BackColor = ThemeTokens.BgApp;
            this.Font = ThemeTokens.FontBody;

            InitializeUi();
            WireEvents();
            LoadConfigToUi();
            RefreshOpenWindows();
            GenerateItemsAndRefreshList(false);
            UpdateExecutionUi(AutoSequenceState.Idle, 0, _currentItems.Count, "Siap dijalankan.");
        }

        private void InitializeUi()
        {
            TableLayoutPanel mainGrid = new TableLayoutPanel();
            mainGrid.Dock = DockStyle.Fill;
            mainGrid.ColumnCount = 2;
            mainGrid.RowCount = 1;
            mainGrid.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);
            mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46f));
            mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54f));
            mainGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel leftCol = CreateLeftColumn();
            leftCol.Margin = ThemeTokens.ScaledPadding(0, 0, 8, 0);

            Panel rightCol = CreateRightColumn();
            rightCol.Margin = ThemeTokens.ScaledPadding(8, 0, 0, 0);

            mainGrid.Controls.Add(leftCol, 0, 0);
            mainGrid.Controls.Add(rightCol, 1, 0);
            this.Controls.Add(mainGrid);
        }

        private Panel CreateLeftColumn()
        {
            TableLayoutPanel stack = new TableLayoutPanel();
            stack.Dock = DockStyle.Fill;
            stack.ColumnCount = 1;
            stack.RowCount = 2;
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel genCard = CreateGeneratorCard();
            genCard.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            Panel queueCard = CreateQueueCard();

            stack.Controls.Add(genCard, 0, 0);
            stack.Controls.Add(queueCard, 0, 1);
            return stack;
        }

        private Panel CreateGeneratorCard()
        {
            Panel card = CreateCardPanel();
            card.AutoSize = true;
            card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 1;

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "Generator Pola Urutan";
            lblTitle.Font = ThemeTokens.FontSection;
            lblTitle.ForeColor = ThemeTokens.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);

            FlowLayoutPanel modeFlow = new FlowLayoutPanel();
            modeFlow.AutoSize = true;
            modeFlow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            modeFlow.WrapContents = true;
            modeFlow.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);

            _rbModeLetter = new RadioButton();
            _rbModeLetter.UseMnemonic = false;
            _rbModeLetter.Text = "Huruf (77a - 77z)";
            _rbModeLetter.AutoSize = true;
            _rbModeLetter.Margin = ThemeTokens.ScaledPadding(0, 0, 12, 0);

            _rbModeNumber = new RadioButton();
            _rbModeNumber.UseMnemonic = false;
            _rbModeNumber.Text = "Angka (1 - 100)";
            _rbModeNumber.AutoSize = true;
            _rbModeNumber.Margin = ThemeTokens.ScaledPadding(0, 0, 12, 0);

            _rbModeCustom = new RadioButton();
            _rbModeCustom.UseMnemonic = false;
            _rbModeCustom.Text = "Daftar Manual";
            _rbModeCustom.AutoSize = true;

            modeFlow.Controls.Add(_rbModeLetter);
            modeFlow.Controls.Add(_rbModeNumber);
            modeFlow.Controls.Add(_rbModeCustom);

            // Letter Pattern Panel
            _pnlLetterInputs = new Panel();
            _pnlLetterInputs.AutoSize = true;
            _pnlLetterInputs.Dock = DockStyle.Top;

            TableLayoutPanel letterGrid = new TableLayoutPanel();
            letterGrid.Dock = DockStyle.Top;
            letterGrid.AutoSize = true;
            letterGrid.ColumnCount = 4;
            letterGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            letterGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            letterGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            letterGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _txtLetterPrefix = new TextBox();
            _txtLetterPrefix.Dock = DockStyle.Fill;
            _txtLetterSuffix = new TextBox();
            _txtLetterSuffix.Dock = DockStyle.Fill;
            _txtLetterStart = new TextBox();
            _txtLetterStart.Dock = DockStyle.Fill;
            _txtLetterEnd = new TextBox();
            _txtLetterEnd.Dock = DockStyle.Fill;

            letterGrid.Controls.Add(MakeFieldLabel("Awalan:"), 0, 0);
            letterGrid.Controls.Add(_txtLetterPrefix, 1, 0);
            letterGrid.Controls.Add(MakeFieldLabel("Akhiran:"), 2, 0);
            letterGrid.Controls.Add(_txtLetterSuffix, 3, 0);

            letterGrid.Controls.Add(MakeFieldLabel("Huruf Dari:"), 0, 1);
            letterGrid.Controls.Add(_txtLetterStart, 1, 1);
            letterGrid.Controls.Add(MakeFieldLabel("Sampai:"), 2, 1);
            letterGrid.Controls.Add(_txtLetterEnd, 3, 1);

            _pnlLetterInputs.Controls.Add(letterGrid);

            // Number Pattern Panel
            _pnlNumberInputs = new Panel();
            _pnlNumberInputs.AutoSize = true;
            _pnlNumberInputs.Dock = DockStyle.Top;

            TableLayoutPanel numGrid = new TableLayoutPanel();
            numGrid.Dock = DockStyle.Top;
            numGrid.AutoSize = true;
            numGrid.ColumnCount = 6;
            numGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            numGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            numGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            numGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            numGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            numGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));

            _txtNumPrefix = new TextBox();
            _txtNumPrefix.Dock = DockStyle.Fill;
            _txtNumSuffix = new TextBox();
            _txtNumSuffix.Dock = DockStyle.Fill;
            _numStart = new NumericUpDown();
            _numStart.Minimum = 0;
            _numStart.Maximum = 999999;
            _numStart.Dock = DockStyle.Fill;
            _numEnd = new NumericUpDown();
            _numEnd.Minimum = 0;
            _numEnd.Maximum = 999999;
            _numEnd.Dock = DockStyle.Fill;
            _numPad = new NumericUpDown();
            _numPad.Minimum = 0;
            _numPad.Maximum = 8;
            _numPad.Dock = DockStyle.Fill;

            numGrid.Controls.Add(MakeFieldLabel("Awalan:"), 0, 0);
            numGrid.Controls.Add(_txtNumPrefix, 1, 0);
            numGrid.Controls.Add(MakeFieldLabel("Akhiran:"), 2, 0);
            numGrid.Controls.Add(_txtNumSuffix, 3, 0);
            numGrid.Controls.Add(MakeFieldLabel("Digit 0:"), 4, 0);
            numGrid.Controls.Add(_numPad, 5, 0);

            numGrid.Controls.Add(MakeFieldLabel("Dari:"), 0, 1);
            numGrid.Controls.Add(_numStart, 1, 1);
            numGrid.Controls.Add(MakeFieldLabel("Sampai:"), 2, 1);
            numGrid.Controls.Add(_numEnd, 3, 1);

            _pnlNumberInputs.Controls.Add(numGrid);

            // Custom List Panel
            _pnlCustomInputs = new Panel();
            _pnlCustomInputs.AutoSize = true;
            _pnlCustomInputs.Dock = DockStyle.Top;

            _txtCustomLines = new TextBox();
            _txtCustomLines.Multiline = true;
            _txtCustomLines.ScrollBars = ScrollBars.Vertical;
            _txtCustomLines.Height = ThemeTokens.Scale(74);
            _txtCustomLines.Dock = DockStyle.Top;
            _pnlCustomInputs.Controls.Add(_txtCustomLines);

            _btnGenerate = new Button();
            _btnGenerate.Text = "Generate & Muat ke Antrean";
            ThemeTokens.StyleSecondaryButton(_btnGenerate);
            _btnGenerate.Anchor = AnchorStyles.Right;
            _btnGenerate.Margin = ThemeTokens.ScaledPadding(0, 8, 0, 0);

            layout.Controls.Add(lblTitle);
            layout.Controls.Add(modeFlow);
            layout.Controls.Add(_pnlLetterInputs);
            layout.Controls.Add(_pnlNumberInputs);
            layout.Controls.Add(_pnlCustomInputs);
            layout.Controls.Add(_btnGenerate);

            card.Controls.Add(layout);
            return card;
        }

        private Panel CreateQueueCard()
        {
            Panel card = CreateCardPanel();
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            TableLayoutPanel topBar = new TableLayoutPanel();
            topBar.Dock = DockStyle.Fill;
            topBar.AutoSize = true;
            topBar.ColumnCount = 2;
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topBar.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);

            _lblQueueInfo = new Label();
            _lblQueueInfo.UseMnemonic = false;
            _lblQueueInfo.Text = "Antrean (0 item)";
            _lblQueueInfo.Font = ThemeTokens.FontSection;
            _lblQueueInfo.ForeColor = ThemeTokens.TextPrimary;
            _lblQueueInfo.AutoSize = true;
            _lblQueueInfo.Anchor = AnchorStyles.Left;

            FlowLayoutPanel qActions = new FlowLayoutPanel();
            qActions.AutoSize = true;
            qActions.WrapContents = false;
            qActions.Anchor = AnchorStyles.Right;

            _btnCopyToManual = new Button();
            _btnCopyToManual.Text = "Edit Manual";
            ThemeTokens.StyleSecondaryButton(_btnCopyToManual);
            _btnCopyToManual.Margin = ThemeTokens.ScaledPadding(0, 0, 6, 0);

            _btnResetIndex = new Button();
            _btnResetIndex.Text = "Reset ke #1";
            ThemeTokens.StyleSecondaryButton(_btnResetIndex);
            _btnResetIndex.Margin = new Padding(0);

            qActions.Controls.Add(_btnCopyToManual);
            qActions.Controls.Add(_btnResetIndex);

            topBar.Controls.Add(_lblQueueInfo, 0, 0);
            topBar.Controls.Add(qActions, 1, 0);

            _lstQueue = new ListBox();
            _lstQueue.Dock = DockStyle.Fill;
            _lstQueue.IntegralHeight = false;
            _lstQueue.Font = ThemeTokens.FontBody;
            _lstQueue.BackColor = ThemeTokens.BgSurface;

            Label lblQueueHint = new Label();
            lblQueueHint.UseMnemonic = false;
            lblQueueHint.Text = "Klik salah satu baris jika ingin memulai urutan dari posisi tersebut.";
            lblQueueHint.Font = ThemeTokens.FontCaption;
            lblQueueHint.ForeColor = ThemeTokens.TextMuted;
            lblQueueHint.AutoSize = true;
            lblQueueHint.Margin = ThemeTokens.ScaledPadding(0, 6, 0, 0);

            layout.Controls.Add(topBar, 0, 0);
            layout.Controls.Add(_lstQueue, 0, 1);
            layout.Controls.Add(lblQueueHint, 0, 2);

            card.Controls.Add(layout);
            return card;
        }

        private Panel CreateRightColumn()
        {
            TableLayoutPanel stack = new TableLayoutPanel();
            stack.Dock = DockStyle.Fill;
            stack.ColumnCount = 1;
            stack.RowCount = 2;
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel settingsCard = CreateExecutionSettingsCard();
            settingsCard.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            Panel controlCard = CreateControlCard();

            stack.Controls.Add(settingsCard, 0, 0);
            stack.Controls.Add(controlCard, 0, 1);
            return stack;
        }

        private Panel CreateExecutionSettingsCard()
        {
            Panel card = CreateCardPanel();
            card.AutoSize = true;
            card.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Top;
            grid.AutoSize = true;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.ColumnCount = 2;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "Pengaturan Eksekusi & Timing";
            lblTitle.Font = ThemeTokens.FontSection;
            lblTitle.ForeColor = ThemeTokens.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);
            grid.SetColumnSpan(lblTitle, 2);
            grid.Controls.Add(lblTitle, 0, 0);

            // Row 1: Command Template
            _txtTemplate = new TextBox();
            _txtTemplate.Dock = DockStyle.Fill;
            _txtTemplate.Margin = ThemeTokens.ScaledPadding(8, 3, 0, 4);
            grid.Controls.Add(MakeFieldLabel("Format Perintah ({item}):"), 0, 1);
            grid.Controls.Add(_txtTemplate, 1, 1);

            // Row 2: Prefix + Prefix Delay
            TableLayoutPanel prefixRow = new TableLayoutPanel();
            prefixRow.Dock = DockStyle.Fill;
            prefixRow.AutoSize = true;
            prefixRow.ColumnCount = 4;
            prefixRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            prefixRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            prefixRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            prefixRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemeTokens.Scale(74)));
            prefixRow.Margin = ThemeTokens.ScaledPadding(8, 2, 0, 2);

            _txtPrefixKey = new TextBox();
            _txtPrefixKey.ReadOnly = true;
            _txtPrefixKey.BackColor = ThemeTokens.BgSubtle;
            _txtPrefixKey.Dock = DockStyle.Fill;
            _txtPrefixKey.Margin = ThemeTokens.ScaledPadding(0, 4, 6, 2);

            _btnPickPrefix = new Button();
            _btnPickPrefix.Text = "Prefix...";
            ThemeTokens.StyleSecondaryButton(_btnPickPrefix);
            _btnPickPrefix.Margin = ThemeTokens.ScaledPadding(0, 2, 10, 2);

            _numPrefixDelay = new NumericUpDown();
            _numPrefixDelay.Minimum = 0;
            _numPrefixDelay.Maximum = 2000;
            _numPrefixDelay.Increment = 20;
            _numPrefixDelay.Dock = DockStyle.Fill;
            _numPrefixDelay.Margin = ThemeTokens.ScaledPadding(6, 4, 0, 2);

            prefixRow.Controls.Add(_txtPrefixKey, 0, 0);
            prefixRow.Controls.Add(_btnPickPrefix, 1, 0);
            prefixRow.Controls.Add(MakeFieldLabel("Jeda Prefix (ms):"), 2, 0);
            prefixRow.Controls.Add(_numPrefixDelay, 3, 0);

            grid.Controls.Add(MakeFieldLabel("Prefix (Pembuka):"), 0, 2);
            grid.Controls.Add(prefixRow, 1, 2);

            // Row 3: Suffix + Typing Delay
            TableLayoutPanel suffixRow = new TableLayoutPanel();
            suffixRow.Dock = DockStyle.Fill;
            suffixRow.AutoSize = true;
            suffixRow.ColumnCount = 3;
            suffixRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            suffixRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            suffixRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemeTokens.Scale(74)));
            suffixRow.Margin = ThemeTokens.ScaledPadding(8, 2, 0, 2);

            _cmbSuffix = new ComboBox();
            _cmbSuffix.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbSuffix.Dock = DockStyle.Fill;
            _cmbSuffix.Items.Add("Tidak ada");
            _cmbSuffix.Items.Add("Enter");
            _cmbSuffix.Items.Add("Tab");
            _cmbSuffix.Items.Add("Space");
            _cmbSuffix.Margin = ThemeTokens.ScaledPadding(0, 3, 10, 2);

            _numTypingDelay = new NumericUpDown();
            _numTypingDelay.Minimum = 0;
            _numTypingDelay.Maximum = 100;
            _numTypingDelay.Dock = DockStyle.Fill;
            _numTypingDelay.Margin = ThemeTokens.ScaledPadding(6, 4, 0, 2);

            suffixRow.Controls.Add(_cmbSuffix, 0, 0);
            suffixRow.Controls.Add(MakeFieldLabel("Delay Huruf (ms):"), 1, 0);
            suffixRow.Controls.Add(_numTypingDelay, 2, 0);

            grid.Controls.Add(MakeFieldLabel("Suffix (Akhiran):"), 0, 3);
            grid.Controls.Add(suffixRow, 1, 3);

            // Row 4: Interval + Countdown
            TableLayoutPanel timingRow = new TableLayoutPanel();
            timingRow.Dock = DockStyle.Fill;
            timingRow.AutoSize = true;
            timingRow.ColumnCount = 4;
            timingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemeTokens.Scale(88)));
            timingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            timingRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            timingRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ThemeTokens.Scale(68)));
            timingRow.Margin = ThemeTokens.ScaledPadding(8, 2, 0, 2);

            _numIntervalMs = new NumericUpDown();
            _numIntervalMs.Minimum = 100;
            _numIntervalMs.Maximum = 300000;
            _numIntervalMs.Increment = 250;
            _numIntervalMs.Dock = DockStyle.Fill;
            _numIntervalMs.Margin = ThemeTokens.ScaledPadding(0, 4, 8, 2);

            _lblIntervalSeconds = new Label();
            _lblIntervalSeconds.UseMnemonic = false;
            _lblIntervalSeconds.Text = "(= 3.5 dtk)";
            _lblIntervalSeconds.AutoSize = true;
            _lblIntervalSeconds.Anchor = AnchorStyles.Left;
            _lblIntervalSeconds.ForeColor = ThemeTokens.TextSecondary;

            _numCountdownSec = new NumericUpDown();
            _numCountdownSec.Minimum = 0;
            _numCountdownSec.Maximum = 30;
            _numCountdownSec.Dock = DockStyle.Fill;
            _numCountdownSec.Margin = ThemeTokens.ScaledPadding(6, 4, 0, 2);

            timingRow.Controls.Add(_numIntervalMs, 0, 0);
            timingRow.Controls.Add(_lblIntervalSeconds, 1, 0);
            timingRow.Controls.Add(MakeFieldLabel("Countdown (dtk):"), 2, 0);
            timingRow.Controls.Add(_numCountdownSec, 3, 0);

            grid.Controls.Add(MakeFieldLabel("Jeda Antar Item (ms):"), 0, 4);
            grid.Controls.Add(timingRow, 1, 4);

            // Row 5: Loop Checkbox
            _chkLoop = new CheckBox();
            _chkLoop.UseMnemonic = false;
            _chkLoop.Text = "Ulangi terus dari awal (Loop) setelah mencapai item terakhir";
            _chkLoop.AutoSize = true;
            _chkLoop.Margin = ThemeTokens.ScaledPadding(8, 4, 0, 4);
            grid.SetColumnSpan(_chkLoop, 2);
            grid.Controls.Add(_chkLoop, 0, 5);

            // Row 6: Target Window Guard
            _chkRequireWindow = new CheckBox();
            _chkRequireWindow.UseMnemonic = false;
            _chkRequireWindow.Text = "Hanya ketik jika jendela fokus:";
            _chkRequireWindow.AutoSize = true;
            _chkRequireWindow.Anchor = AnchorStyles.Left;
            _chkRequireWindow.Margin = ThemeTokens.ScaledPadding(0, 4, 0, 2);

            TableLayoutPanel winRow = new TableLayoutPanel();
            winRow.Dock = DockStyle.Fill;
            winRow.AutoSize = true;
            winRow.ColumnCount = 2;
            winRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            winRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            winRow.Margin = ThemeTokens.ScaledPadding(8, 2, 0, 0);

            _cmbTargetWindow = new ComboBox();
            _cmbTargetWindow.DropDownStyle = ComboBoxStyle.DropDown;
            _cmbTargetWindow.Dock = DockStyle.Fill;
            _cmbTargetWindow.Margin = ThemeTokens.ScaledPadding(0, 3, 8, 2);

            _btnRefreshWindows = new Button();
            _btnRefreshWindows.Text = "Refresh";
            ThemeTokens.StyleSecondaryButton(_btnRefreshWindows);
            _btnRefreshWindows.Margin = ThemeTokens.ScaledPadding(0, 1, 0, 1);

            winRow.Controls.Add(_cmbTargetWindow, 0, 0);
            winRow.Controls.Add(_btnRefreshWindows, 1, 0);

            grid.Controls.Add(_chkRequireWindow, 0, 6);
            grid.Controls.Add(winRow, 1, 6);

            card.Controls.Add(grid);
            return card;
        }

        private Panel CreateControlCard()
        {
            Panel card = CreateCardPanel();
            card.Padding = ThemeTokens.ScaledPadding(12, 10, 12, 10);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblTitle = new Label();
            lblTitle.UseMnemonic = false;
            lblTitle.Text = "Kontrol Otomatis & Hotkey Cepat";
            lblTitle.Font = ThemeTokens.FontSection;
            lblTitle.ForeColor = ThemeTokens.TextPrimary;
            lblTitle.AutoSize = true;
            lblTitle.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 6);

            // Hotkeys row
            TableLayoutPanel hotkeyRow = new TableLayoutPanel();
            hotkeyRow.Dock = DockStyle.Fill;
            hotkeyRow.AutoSize = true;
            hotkeyRow.ColumnCount = 6;
            hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            hotkeyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            hotkeyRow.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            _txtPauseHotkey = new TextBox();
            _txtPauseHotkey.ReadOnly = true;
            _txtPauseHotkey.BackColor = ThemeTokens.BgSubtle;
            _txtPauseHotkey.Font = ThemeTokens.FontMono;
            _txtPauseHotkey.Dock = DockStyle.Fill;
            _txtPauseHotkey.Margin = ThemeTokens.ScaledPadding(6, 3, 4, 2);

            _btnPickPauseHotkey = new Button();
            _btnPickPauseHotkey.Text = "Ubah...";
            ThemeTokens.StyleSecondaryButton(_btnPickPauseHotkey);
            _btnPickPauseHotkey.Margin = ThemeTokens.ScaledPadding(0, 1, 12, 1);

            _txtStopHotkey = new TextBox();
            _txtStopHotkey.ReadOnly = true;
            _txtStopHotkey.BackColor = ThemeTokens.BgSubtle;
            _txtStopHotkey.Font = ThemeTokens.FontMono;
            _txtStopHotkey.Dock = DockStyle.Fill;
            _txtStopHotkey.Margin = ThemeTokens.ScaledPadding(6, 3, 4, 2);

            _btnPickStopHotkey = new Button();
            _btnPickStopHotkey.Text = "Ubah...";
            ThemeTokens.StyleSecondaryButton(_btnPickStopHotkey);
            _btnPickStopHotkey.Margin = ThemeTokens.ScaledPadding(0, 1, 0, 1);

            hotkeyRow.Controls.Add(MakeFieldLabel("Pause/Resume:"), 0, 0);
            hotkeyRow.Controls.Add(_txtPauseHotkey, 1, 0);
            hotkeyRow.Controls.Add(_btnPickPauseHotkey, 2, 0);
            hotkeyRow.Controls.Add(MakeFieldLabel("Stop:"), 3, 0);
            hotkeyRow.Controls.Add(_txtStopHotkey, 4, 0);
            hotkeyRow.Controls.Add(_btnPickStopHotkey, 5, 0);

            _lblStepPreview = new Label();
            _lblStepPreview.UseMnemonic = false;
            _lblStepPreview.Font = ThemeTokens.FontCaption;
            _lblStepPreview.ForeColor = ThemeTokens.TextSecondary;
            _lblStepPreview.AutoSize = true;
            _lblStepPreview.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            _lblRunStatus = new Label();
            _lblRunStatus.UseMnemonic = false;
            _lblRunStatus.Dock = DockStyle.Fill;
            _lblRunStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblRunStatus.Font = ThemeTokens.FontBodyBold;
            _lblRunStatus.BackColor = ThemeTokens.NeutralBadgeBg;
            _lblRunStatus.ForeColor = ThemeTokens.TextPrimary;
            _lblRunStatus.MinimumSize = new Size(0, ThemeTokens.Scale(42));
            _lblRunStatus.Margin = ThemeTokens.ScaledPadding(0, 0, 0, 8);

            TableLayoutPanel btnBar = new TableLayoutPanel();
            btnBar.Dock = DockStyle.Fill;
            btnBar.AutoSize = true;
            btnBar.ColumnCount = 2;
            btnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            btnBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            _btnStopAuto = new Button();
            _btnStopAuto.Text = "Stop";
            ThemeTokens.StyleSecondaryButton(_btnStopAuto);
            _btnStopAuto.MinimumSize = new Size(ThemeTokens.Scale(100), ThemeTokens.Scale(34));
            _btnStopAuto.Anchor = AnchorStyles.Left;
            _btnStopAuto.Enabled = false;

            _btnStartPauseAuto = new Button();
            _btnStartPauseAuto.Text = "Mulai Auto";
            ThemeTokens.StylePrimaryButton(_btnStartPauseAuto);
            _btnStartPauseAuto.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
            _btnStartPauseAuto.Anchor = AnchorStyles.Right;

            btnBar.Controls.Add(_btnStopAuto, 0, 0);
            btnBar.Controls.Add(_btnStartPauseAuto, 1, 0);

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(hotkeyRow, 0, 1);
            layout.Controls.Add(_lblStepPreview, 0, 2);
            layout.Controls.Add(_lblRunStatus, 0, 3);
            layout.Controls.Add(btnBar, 0, 4);

            card.Controls.Add(layout);
            return card;
        }

        private void WireEvents()
        {
            _rbModeLetter.CheckedChanged += OnPatternModeChanged;
            _rbModeNumber.CheckedChanged += OnPatternModeChanged;
            _rbModeCustom.CheckedChanged += OnPatternModeChanged;

            _btnGenerate.Click += delegate
            {
                if (GenerateItemsAndRefreshList(true) && _autoService.State == AutoSequenceState.Paused)
                {
                    _autoService.UpdateItemsWhilePaused(_currentItems, _startIndex);
                }
            };
            _btnCopyToManual.Click += OnCopyToManualClick;
            _btnResetIndex.Click += delegate
            {
                _startIndex = 0;
                if (_lstQueue.Items.Count > 0)
                {
                    _isUpdatingQueueSelection = true;
                    _lstQueue.SelectedIndex = 0;
                    _isUpdatingQueueSelection = false;
                }

                if (_autoService.State == AutoSequenceState.Paused)
                {
                    _autoService.SetNextIndex(0);
                }
                UpdateQueueHeader();
                UpdateStepPreview();
            };

            _lstQueue.SelectedIndexChanged += delegate
            {
                if (_isUpdatingQueueSelection || _lstQueue.SelectedIndex < 0) return;

                if (_autoService.State == AutoSequenceState.Paused)
                {
                    _startIndex = _lstQueue.SelectedIndex;
                    _autoService.SetNextIndex(_startIndex);
                    UpdateQueueHeader();
                    UpdateStepPreview();
                }
                else if (!_autoService.IsRunningOrPaused)
                {
                    _startIndex = _lstQueue.SelectedIndex;
                    UpdateQueueHeader();
                    UpdateStepPreview();
                }
            };

            _txtTemplate.TextChanged += OnSettingsChanged;
            _numPrefixDelay.ValueChanged += OnSettingsChanged;
            _cmbSuffix.SelectedIndexChanged += OnSettingsChanged;
            _numTypingDelay.ValueChanged += OnSettingsChanged;
            _numIntervalMs.ValueChanged += OnSettingsChanged;
            _numCountdownSec.ValueChanged += OnSettingsChanged;
            _chkLoop.CheckedChanged += OnSettingsChanged;
            _chkRequireWindow.CheckedChanged += OnSettingsChanged;
            _cmbTargetWindow.TextChanged += OnSettingsChanged;
            _btnRefreshWindows.Click += delegate { RefreshOpenWindows(); };

            _txtLetterPrefix.TextChanged += OnGeneratorInputChanged;
            _txtLetterSuffix.TextChanged += OnGeneratorInputChanged;
            _txtLetterStart.TextChanged += OnGeneratorInputChanged;
            _txtLetterEnd.TextChanged += OnGeneratorInputChanged;
            _txtNumPrefix.TextChanged += OnGeneratorInputChanged;
            _txtNumSuffix.TextChanged += OnGeneratorInputChanged;
            _numStart.ValueChanged += OnGeneratorInputChanged;
            _numEnd.ValueChanged += OnGeneratorInputChanged;
            _numPad.ValueChanged += OnGeneratorInputChanged;
            _txtCustomLines.TextChanged += OnGeneratorInputChanged;

            _btnPickPrefix.Click += OnPickPrefixClick;
            _btnPickPauseHotkey.Click += OnPickPauseHotkeyClick;
            _btnPickStopHotkey.Click += OnPickStopHotkeyClick;

            _btnStartPauseAuto.Click += OnStartPauseClick;
            _btnStopAuto.Click += delegate { _autoService.Stop(); };

            _autoService.ProgressChanged += OnAutoSequenceProgressChanged;
        }

        private void OnPatternModeChanged(object sender, EventArgs e)
        {
            UpdatePatternPanelsVisibility();
            if (!_isLoading)
            {
                SaveUiToConfig();
                GenerateItemsAndRefreshList(false);
            }
        }

        private void OnGeneratorInputChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            SaveUiToConfig();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;
            _cmbTargetWindow.Enabled = _chkRequireWindow.Checked;
            _btnRefreshWindows.Enabled = _chkRequireWindow.Checked;
            _lblIntervalSeconds.Text = string.Format("(= {0:0.0} dtk)", (double)_numIntervalMs.Value / 1000.0);
            SaveUiToConfig();
            UpdateStepPreview();
        }

        private void OnCopyToManualClick(object sender, EventArgs e)
        {
            if (_currentItems.Count == 0) return;
            _txtCustomLines.Text = string.Join("\r\n", _currentItems.ToArray());
            _rbModeCustom.Checked = true;
            SaveUiToConfig();
            GenerateItemsAndRefreshList(false);
            NotifyFooter("Daftar disalin ke mode Daftar Manual. Anda bisa mengedit item per baris.");
        }

        private void OnPickPrefixClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Pilih Prefix Key (Pembuka)",
                    _config.AutoSequence.PrefixVirtualKey,
                    false,
                    false,
                    false,
                    false,
                    true))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _config.AutoSequence.PrefixVirtualKey = dlg.SelectedVirtualKey;
                        _txtPrefixKey.Text = dlg.SelectedVirtualKey > 0
                            ? KeybindItem.FormatKeyName(dlg.SelectedVirtualKey)
                            : "(Tidak ada)";
                        SaveConfigAndPreview();
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnPickPauseHotkeyClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                AutoSequenceConfig s = _config.AutoSequence;
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Atur Hotkey Pause/Resume Auto",
                    s.PauseResumeVirtualKey,
                    s.PauseResumeCtrl,
                    s.PauseResumeAlt,
                    s.PauseResumeShift,
                    true,
                    false))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedVirtualKey > 0)
                    {
                        s.PauseResumeVirtualKey = dlg.SelectedVirtualKey;
                        s.PauseResumeCtrl = dlg.SelectedCtrl;
                        s.PauseResumeAlt = dlg.SelectedAlt;
                        s.PauseResumeShift = dlg.SelectedShift;
                        _txtPauseHotkey.Text = KeybindItem.FormatHotkey(
                            s.PauseResumeVirtualKey,
                            s.PauseResumeCtrl,
                            s.PauseResumeAlt,
                            s.PauseResumeShift);
                        SaveConfigAndPreview();
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnPickStopHotkeyClick(object sender, EventArgs e)
        {
            _hookService.SetCaptureSuspend(true);
            try
            {
                AutoSequenceConfig s = _config.AutoSequence;
                using (KeyCaptureDialog dlg = new KeyCaptureDialog(
                    "Atur Hotkey Stop Auto",
                    s.StopVirtualKey,
                    s.StopCtrl,
                    s.StopAlt,
                    s.StopShift,
                    true,
                    false))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedVirtualKey > 0)
                    {
                        s.StopVirtualKey = dlg.SelectedVirtualKey;
                        s.StopCtrl = dlg.SelectedCtrl;
                        s.StopAlt = dlg.SelectedAlt;
                        s.StopShift = dlg.SelectedShift;
                        _txtStopHotkey.Text = KeybindItem.FormatHotkey(
                            s.StopVirtualKey,
                            s.StopCtrl,
                            s.StopAlt,
                            s.StopShift);
                        SaveConfigAndPreview();
                    }
                }
            }
            finally
            {
                _hookService.SetCaptureSuspend(false);
            }
        }

        private void OnStartPauseClick(object sender, EventArgs e)
        {
            AutoSequenceState current = _autoService.State;
            if (current == AutoSequenceState.Paused)
            {
                // Resuming via UI button gives countdown seconds to switch back to target window
                _autoService.Resume(_config.AutoSequence.CountdownSeconds > 0);
                return;
            }

            if (_autoService.IsRunningOrPaused)
            {
                _autoService.Pause();
                return;
            }

            SaveUiToConfig();
            if (!GenerateItemsAndRefreshList(true))
            {
                return;
            }

            if (_startIndex >= _currentItems.Count)
            {
                _startIndex = 0;
            }

            try
            {
                _autoService.Start(_currentItems, _startIndex, _config);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menjalankan Auto Sequence:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnAutoSequenceProgressChanged(AutoSequenceProgressEventArgs args)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<AutoSequenceProgressEventArgs>(ApplyProgressToUi), args);
            }
            else
            {
                ApplyProgressToUi(args);
            }
        }

        private void ApplyProgressToUi(AutoSequenceProgressEventArgs args)
        {
            if (args.CurrentIndex >= 0 && args.CurrentIndex < _lstQueue.Items.Count)
            {
                _startIndex = args.CurrentIndex;
                _isUpdatingQueueSelection = true;
                _lstQueue.SelectedIndex = args.CurrentIndex;
                _isUpdatingQueueSelection = false;
            }
            else if (args.State == AutoSequenceState.Completed)
            {
                _startIndex = 0;
            }

            UpdateQueueHeader();
            UpdateStepPreview();
            UpdateExecutionUi(args.State, args.CurrentIndex, args.TotalCount, args.StatusMessage);
            NotifyFooter(args.StatusMessage);
        }

        private void UpdateExecutionUi(AutoSequenceState state, int currentIdx, int total, string message)
        {
            bool busy = (state == AutoSequenceState.Countdown ||
                         state == AutoSequenceState.Running ||
                         state == AutoSequenceState.Paused ||
                         state == AutoSequenceState.WaitingWindow);
            bool activelyRunning = (state == AutoSequenceState.Countdown ||
                                    state == AutoSequenceState.Running ||
                                    state == AutoSequenceState.WaitingWindow);

            _btnGenerate.Enabled = !activelyRunning;
            _btnStartPauseAuto.Enabled = (_currentItems.Count > 0) || busy;
            _btnStopAuto.Enabled = busy;
            _btnResetIndex.Enabled = !activelyRunning;

            switch (state)
            {
                case AutoSequenceState.Countdown:
                case AutoSequenceState.Running:
                    _lblRunStatus.BackColor = ThemeTokens.SuccessBg;
                    _lblRunStatus.ForeColor = ThemeTokens.SuccessText;
                    _lblRunStatus.Text = message;
                    _btnStartPauseAuto.Text = "Pause (" + _txtPauseHotkey.Text + ")";
                    ThemeTokens.StyleSecondaryButton(_btnStartPauseAuto);
                    _btnStartPauseAuto.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
                    break;

                case AutoSequenceState.WaitingWindow:
                case AutoSequenceState.Paused:
                    _lblRunStatus.BackColor = ThemeTokens.WarningBg;
                    _lblRunStatus.ForeColor = ThemeTokens.WarningText;
                    _lblRunStatus.Text = message;
                    _btnStartPauseAuto.Text = state == AutoSequenceState.Paused ? "Lanjutkan" : "Pause";
                    ThemeTokens.StylePrimaryButton(_btnStartPauseAuto);
                    _btnStartPauseAuto.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
                    break;

                case AutoSequenceState.Completed:
                    _lblRunStatus.BackColor = ThemeTokens.SuccessBg;
                    _lblRunStatus.ForeColor = ThemeTokens.SuccessText;
                    _lblRunStatus.Text = message;
                    _btnStartPauseAuto.Text = "Mulai Lagi";
                    ThemeTokens.StylePrimaryButton(_btnStartPauseAuto);
                    _btnStartPauseAuto.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
                    break;

                default:
                    _lblRunStatus.BackColor = ThemeTokens.NeutralBadgeBg;
                    _lblRunStatus.ForeColor = ThemeTokens.TextPrimary;
                    _lblRunStatus.Text = string.IsNullOrEmpty(message) ? "Siap dijalankan." : message;
                    _btnStartPauseAuto.Text = "Mulai Auto";
                    ThemeTokens.StylePrimaryButton(_btnStartPauseAuto);
                    _btnStartPauseAuto.MinimumSize = new Size(ThemeTokens.Scale(140), ThemeTokens.Scale(34));
                    break;
            }
        }

        private bool GenerateItemsAndRefreshList(bool showValidationErrors)
        {
            SaveUiToConfig();
            SequenceGenerationResult res = SequenceGenerator.Generate(_config.AutoSequence);
            if (!res.Success)
            {
                if (showValidationErrors)
                {
                    MessageBox.Show(res.ErrorMessage, "Pola Tidak Valid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }

            _currentItems = res.Items;
            if (_startIndex >= _currentItems.Count)
            {
                _startIndex = 0;
            }

            _isUpdatingQueueSelection = true;
            _lstQueue.BeginUpdate();
            try
            {
                _lstQueue.Items.Clear();
                for (int i = 0; i < _currentItems.Count; i++)
                {
                    _lstQueue.Items.Add(string.Format("{0}. {1}", i + 1, _currentItems[i]));
                }
                if (_lstQueue.Items.Count > 0)
                {
                    _lstQueue.SelectedIndex = _startIndex;
                }
            }
            finally
            {
                _lstQueue.EndUpdate();
                _isUpdatingQueueSelection = false;
            }

            UpdateQueueHeader();
            UpdateStepPreview();
            return true;
        }

        private void UpdateQueueHeader()
        {
            int pos = _currentItems.Count > 0 ? (_startIndex + 1) : 0;
            _lblQueueInfo.Text = string.Format("Antrean ({0} item • Mulai #{1})", _currentItems.Count, pos);
        }

        private void UpdateStepPreview()
        {
            AutoSequenceConfig s = _config.AutoSequence;
            string sampleItem = (_currentItems.Count > 0 && _startIndex < _currentItems.Count)
                ? _currentItems[_startIndex]
                : "77a";

            string prefixPart = s.PrefixVirtualKey > 0
                ? "Tekan [" + KeybindItem.FormatKeyName(s.PrefixVirtualKey) + "] ➔ " + s.PrefixDelayMs + " ms ➔ "
                : string.Empty;

            string cmd = SequenceGenerator.FormatCommand(s.CommandTemplate, sampleItem);

            KeybindItem tmpSuffix = new KeybindItem { Suffix = s.Suffix };
            string suffixPart = s.Suffix != SuffixAction.None
                ? " ➔ Tekan [" + tmpSuffix.GetSuffixDisplay() + "]"
                : string.Empty;

            _lblStepPreview.Text = string.Format(
                "Simulasi langkah #{0}: {1}Ketik \"{2}\"{3} ➔ tunggu {4:0.0} dtk",
                _startIndex + 1,
                prefixPart,
                cmd,
                suffixPart,
                s.IntervalMs / 1000.0);
        }

        private void LoadConfigToUi()
        {
            _isLoading = true;
            try
            {
                AutoSequenceConfig s = _config.AutoSequence;

                _rbModeLetter.Checked = (s.PatternMode == SequencePatternMode.LetterRange);
                _rbModeNumber.Checked = (s.PatternMode == SequencePatternMode.NumberRange);
                _rbModeCustom.Checked = (s.PatternMode == SequencePatternMode.CustomList);

                _txtLetterPrefix.Text = s.PrefixText ?? string.Empty;
                _txtLetterSuffix.Text = s.SuffixText ?? string.Empty;
                _txtLetterStart.Text = s.LetterStart ?? "a";
                _txtLetterEnd.Text = s.LetterEnd ?? "z";

                _txtNumPrefix.Text = s.PrefixText ?? string.Empty;
                _txtNumSuffix.Text = s.SuffixText ?? string.Empty;
                _numStart.Value = ClampNumeric(_numStart, s.NumberStart);
                _numEnd.Value = ClampNumeric(_numEnd, s.NumberEnd);
                _numPad.Value = ClampNumeric(_numPad, s.NumberPadDigits);

                _txtCustomLines.Text = s.CustomItemsText ?? string.Empty;

                _txtTemplate.Text = s.CommandTemplate ?? "warp {item}";
                _txtPrefixKey.Text = s.PrefixVirtualKey > 0
                    ? KeybindItem.FormatKeyName(s.PrefixVirtualKey)
                    : "(Tidak ada)";
                _numPrefixDelay.Value = ClampNumeric(_numPrefixDelay, s.PrefixDelayMs);
                _cmbSuffix.SelectedIndex = (int)s.Suffix;
                _numTypingDelay.Value = ClampNumeric(_numTypingDelay, s.TypingDelayMs);
                _numIntervalMs.Value = ClampNumeric(_numIntervalMs, s.IntervalMs);
                _lblIntervalSeconds.Text = string.Format("(= {0:0.0} dtk)", (double)_numIntervalMs.Value / 1000.0);
                _numCountdownSec.Value = ClampNumeric(_numCountdownSec, s.CountdownSeconds);
                _chkLoop.Checked = s.LoopForever;

                _chkRequireWindow.Checked = s.RequireTargetWindowFocus;
                _cmbTargetWindow.Text = s.TargetWindowKeyword ?? string.Empty;
                _cmbTargetWindow.Enabled = _chkRequireWindow.Checked;
                _btnRefreshWindows.Enabled = _chkRequireWindow.Checked;

                _txtPauseHotkey.Text = KeybindItem.FormatHotkey(
                    s.PauseResumeVirtualKey,
                    s.PauseResumeCtrl,
                    s.PauseResumeAlt,
                    s.PauseResumeShift);
                _txtStopHotkey.Text = KeybindItem.FormatHotkey(
                    s.StopVirtualKey,
                    s.StopCtrl,
                    s.StopAlt,
                    s.StopShift);

                UpdatePatternPanelsVisibility();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SaveUiToConfig()
        {
            AutoSequenceConfig s = _config.AutoSequence;

            if (_rbModeNumber.Checked)
            {
                s.PatternMode = SequencePatternMode.NumberRange;
                s.PrefixText = _txtNumPrefix.Text ?? string.Empty;
                s.SuffixText = _txtNumSuffix.Text ?? string.Empty;
            }
            else if (_rbModeCustom.Checked)
            {
                s.PatternMode = SequencePatternMode.CustomList;
            }
            else
            {
                s.PatternMode = SequencePatternMode.LetterRange;
                s.PrefixText = _txtLetterPrefix.Text ?? string.Empty;
                s.SuffixText = _txtLetterSuffix.Text ?? string.Empty;
            }

            s.LetterStart = (_txtLetterStart.Text ?? string.Empty).Trim();
            s.LetterEnd = (_txtLetterEnd.Text ?? string.Empty).Trim();
            s.NumberStart = (int)_numStart.Value;
            s.NumberEnd = (int)_numEnd.Value;
            s.NumberPadDigits = (int)_numPad.Value;
            s.CustomItemsText = _txtCustomLines.Text ?? string.Empty;

            s.CommandTemplate = _txtTemplate.Text ?? string.Empty;
            s.PrefixDelayMs = (int)_numPrefixDelay.Value;
            if (_cmbSuffix.SelectedIndex >= 0)
            {
                s.Suffix = (SuffixAction)_cmbSuffix.SelectedIndex;
            }
            s.TypingDelayMs = (int)_numTypingDelay.Value;
            s.IntervalMs = (int)_numIntervalMs.Value;
            s.CountdownSeconds = (int)_numCountdownSec.Value;
            s.LoopForever = _chkLoop.Checked;

            s.RequireTargetWindowFocus = _chkRequireWindow.Checked;
            s.TargetWindowKeyword = _cmbTargetWindow.Text ?? string.Empty;

            if (_saveConfigCallback != null)
            {
                _saveConfigCallback();
            }
        }

        private void SaveConfigAndPreview()
        {
            SaveUiToConfig();
            UpdateStepPreview();
        }

        private void UpdatePatternPanelsVisibility()
        {
            _pnlLetterInputs.Visible = _rbModeLetter.Checked;
            _pnlNumberInputs.Visible = _rbModeNumber.Checked;
            _pnlCustomInputs.Visible = _rbModeCustom.Checked;
        }

        private void RefreshOpenWindows()
        {
            string current = _cmbTargetWindow.Text;
            _cmbTargetWindow.Items.Clear();

            int pid = Process.GetCurrentProcess().Id;
            List<WindowInfo> wins = _windowService.GetOpenWindows(pid);
            for (int i = 0; i < wins.Count; i++)
            {
                _cmbTargetWindow.Items.Add(wins[i].DisplayLabel);
            }
            _cmbTargetWindow.Text = current;
        }

        private void NotifyFooter(string message)
        {
            if (_footerCallback != null)
            {
                _footerCallback(message);
            }
        }

        private static decimal ClampNumeric(NumericUpDown nud, int value)
        {
            if (value < (int)nud.Minimum) return nud.Minimum;
            if (value > (int)nud.Maximum) return nud.Maximum;
            return value;
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

        private static Label MakeFieldLabel(string text)
        {
            Label lbl = new Label();
            lbl.UseMnemonic = false;
            lbl.Text = text;
            lbl.Font = ThemeTokens.FontBody;
            lbl.ForeColor = ThemeTokens.TextSecondary;
            lbl.AutoSize = true;
            lbl.Anchor = AnchorStyles.Left;
            lbl.Margin = ThemeTokens.ScaledPadding(0, 4, 4, 4);
            return lbl;
        }
    }
}
