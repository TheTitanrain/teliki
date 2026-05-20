using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class SettingsForm : Form
    {
        private const int LabelColumnWidth = 170;
        private static readonly Padding SectionMargin = new Padding(0, 0, 0, 12);
        private readonly IReadOnlyList<DisplayScreen> _screens;
        private readonly Func<EditableSettings, bool> _saveHandler;
        private readonly Func<bool> _exitHandler;
        private readonly TextBox _mediaFolderTextBox = new TextBox();
        private readonly NumericUpDown _intervalNumeric = new NumericUpDown();
        private readonly NumericUpDown _scanIntervalNumeric = new NumericUpDown();
        private readonly NumericUpDown _scanTimeoutNumeric = new NumericUpDown();
        private readonly ComboBox _screenModeComboBox = new ComboBox();
        private readonly ComboBox _screenComboBox = new ComboBox();
        private readonly Button _saveButton = new Button();
        private readonly Button _cancelButton = new Button();
        private readonly Button _exitButton = new Button();
        private readonly Button _browseButton = new Button();
        private readonly Label _screenHelperLabel = new Label();
        private readonly IDisposable _cursorScope;
        private bool _dirty;
        private bool _initializing;
        private int _selectedScreenIndex;

        public SettingsForm(
            EditableSettings settings,
            IReadOnlyList<DisplayScreen> screens,
            Func<EditableSettings, bool> saveHandler,
            Func<bool> exitHandler)
        {
            _screens = screens;
            _saveHandler = saveHandler;
            _exitHandler = exitHandler;

            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(660, 500);
            _cursorScope = CursorVisibilityManager.Shared.ShowCursorWhileModalUiOpen();

            ConfigureControls(settings);
            Controls.Add(BuildLayout());

            AcceptButton = _saveButton;
            CancelButton = _cancelButton;
            FormClosing += OnFormClosing;
        }

        public bool ExitApplicationRequested { get; private set; }
        internal string SelectedScreenMode { get { return GetSelectedScreenMode(); } }
        internal int SelectedScreenIndex { get { return _selectedScreenIndex; } }
        internal bool IsScreenSelectorEnabled { get { return _screenComboBox.Enabled; } }
        internal bool IsDirty { get { return _dirty; } }
        internal string ScreenHelperText { get { return _screenHelperLabel.Text; } }
        internal bool HasScreenHelperLabel { get { return _screenHelperLabel.Parent != null; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cursorScope.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ConfigureControls(EditableSettings settings)
        {
            _initializing = true;

            _mediaFolderTextBox.Name = "MediaFolderTextBox";
            _mediaFolderTextBox.Text = settings.MediaFolder;
            _mediaFolderTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _mediaFolderTextBox.TextChanged += delegate { MarkDirty(); };

            ConfigureNumeric(_intervalNumeric, settings.IntervalSeconds);
            ConfigureNumeric(_scanIntervalNumeric, settings.ScanIntervalSeconds);
            ConfigureNumeric(_scanTimeoutNumeric, settings.ScanTimeoutSeconds);
            ConfigureScreenMode(settings.ScreenMode);
            ConfigureScreens(settings.ScreenIndex);
            ConfigureScreenHelperLabel();
            ApplyScreenSelectorState();

            _browseButton.Name = "BrowseButton";
            _browseButton.Text = "Browse...";
            _browseButton.AutoSize = true;
            _browseButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _browseButton.Anchor = AnchorStyles.Left;
            _browseButton.Margin = new Padding(8, 0, 0, 0);
            _browseButton.Click += OnBrowseClick;

            _saveButton.Name = "SaveButton";
            _saveButton.Text = "Save";
            _saveButton.AutoSize = true;
            _saveButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _saveButton.MinimumSize = new Size(96, 0);
            _saveButton.Click += OnSaveClick;

            _cancelButton.Name = "CancelButton";
            _cancelButton.Text = "Cancel";
            _cancelButton.AutoSize = true;
            _cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _cancelButton.MinimumSize = new Size(96, 0);
            _cancelButton.Click += delegate { Close(); };

            _exitButton.Name = "ExitButton";
            _exitButton.Text = "Exit Application";
            _exitButton.AutoSize = true;
            _exitButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _exitButton.MinimumSize = new Size(120, 0);
            _exitButton.Click += OnExitApplicationClick;
            _initializing = false;
        }

        private void ConfigureNumeric(NumericUpDown control, int value)
        {
            control.Name = control.Name ?? string.Empty;
            control.Minimum = 1;
            control.Maximum = 86400;
            control.Value = Math.Max(1, value);
            control.Width = 120;
            control.Anchor = AnchorStyles.Left;
            control.ValueChanged += delegate { MarkDirty(); };
        }

        private void ConfigureScreenMode(string screenMode)
        {
            _screenModeComboBox.Name = "ScreenModeComboBox";
            _screenModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _screenModeComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _screenModeComboBox.Items.Add(new ScreenModeOption("All monitors", DisplayModeParser.AllScreens));
            _screenModeComboBox.Items.Add(new ScreenModeOption("Primary monitor only", DisplayModeParser.PrimaryScreen));
            _screenModeComboBox.Items.Add(new ScreenModeOption("Single monitor", DisplayModeParser.SingleScreen));
            SelectScreenMode(DisplayModeParser.Canonicalize(screenMode));
            _screenModeComboBox.SelectedIndexChanged += delegate
            {
                ApplyScreenSelectorState();
                MarkDirty();
            };
        }

        private void ConfigureScreens(int selectedScreenIndex)
        {
            _screenComboBox.Name = "ScreenComboBox";
            _screenComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _screenComboBox.DisplayMember = "DisplayLabel";
            _screenComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            foreach (var screen in _screens)
            {
                _screenComboBox.Items.Add(screen);
            }

            if (_screens.Count > 0)
            {
                SelectScreenIndex(selectedScreenIndex > 0 ? selectedScreenIndex : _screens[0].Index);
            }

            _screenComboBox.SelectedIndexChanged += delegate
            {
                var selected = _screenComboBox.SelectedItem as DisplayScreen;
                if (selected != null)
                {
                    _selectedScreenIndex = selected.Index;
                }

                MarkDirty();
            };
        }

        private void ConfigureScreenHelperLabel()
        {
            _screenHelperLabel.Name = "ScreenHelperLabel";
            _screenHelperLabel.AutoSize = true;
            _screenHelperLabel.Text = "Target screen is used only when Display mode is set to Single monitor.";
            _screenHelperLabel.Margin = new Padding(0, 4, 0, 0);
            _screenHelperLabel.ForeColor = SystemColors.GrayText;
        }

        private Control BuildLayout()
        {
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(16);
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 1;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(BuildHeader(), 0, 0);
            layout.Controls.Add(BuildMediaSection(), 0, 1);
            layout.Controls.Add(BuildPlaybackSection(), 0, 2);
            layout.Controls.Add(BuildDisplaySection(), 0, 3);
            layout.Controls.Add(BuildFooter(), 0, 4);

            return layout;
        }

        private Control BuildHeader()
        {
            var layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 1;
            layout.Dock = DockStyle.Top;
            layout.Margin = new Padding(0, 0, 0, 16);

            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4),
                Text = "Settings"
            }, 0, 0);

            layout.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Text = "Update playback, scanning, and display settings for this player."
            }, 0, 1);

            return layout;
        }

        private Control BuildMediaSection()
        {
            var group = CreateGroupBox("Media");
            var layout = CreateSectionLayout(3);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            AddRow(layout, 0, "Media folder", _mediaFolderTextBox, _browseButton);

            group.Controls.Add(layout);
            return group;
        }

        private Control BuildPlaybackSection()
        {
            var group = CreateGroupBox("Playback");
            var layout = CreateSectionLayout(2);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _intervalNumeric.Name = "IntervalNumeric";
            _scanIntervalNumeric.Name = "ScanIntervalNumeric";
            _scanTimeoutNumeric.Name = "ScanTimeoutNumeric";

            AddRow(layout, 0, "Playback interval (sec)", _intervalNumeric, null);
            AddRow(layout, 1, "Scan interval (sec)", _scanIntervalNumeric, null);
            AddRow(layout, 2, "Scan timeout (sec)", _scanTimeoutNumeric, null);

            group.Controls.Add(layout);
            return group;
        }

        private Control BuildDisplaySection()
        {
            var group = CreateGroupBox("Display");
            var layout = CreateSectionLayout(2);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidth));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(layout, 0, "Display mode", _screenModeComboBox, null);
            AddRow(layout, 1, "Target screen", _screenComboBox, null);
            layout.Controls.Add(_screenHelperLabel, 1, 2);

            group.Controls.Add(layout);
            return group;
        }

        private Control BuildFooter()
        {
            var layout = new TableLayoutPanel();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.ColumnCount = 3;
            layout.Dock = DockStyle.Top;
            layout.Margin = new Padding(0, 4, 0, 0);
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var leftActions = new FlowLayoutPanel();
            leftActions.AutoSize = true;
            leftActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            leftActions.Dock = DockStyle.Fill;
            leftActions.Margin = new Padding(0);
            leftActions.Controls.Add(_exitButton);

            var rightActions = new FlowLayoutPanel();
            rightActions.AutoSize = true;
            rightActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rightActions.Dock = DockStyle.Fill;
            rightActions.FlowDirection = FlowDirection.RightToLeft;
            rightActions.Margin = new Padding(0);
            rightActions.Controls.Add(_saveButton);
            rightActions.Controls.Add(_cancelButton);

            layout.Controls.Add(leftActions, 0, 0);
            layout.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }, 1, 0);
            layout.Controls.Add(rightActions, 2, 0);

            return layout;
        }

        private GroupBox CreateGroupBox(string text)
        {
            return new GroupBox
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Margin = SectionMargin,
                Padding = new Padding(12, 10, 12, 12),
                Text = text
            };
        }

        private TableLayoutPanel CreateSectionLayout(int columnCount)
        {
            return new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = columnCount,
                Dock = DockStyle.Top,
                Margin = new Padding(0)
            };
        }

        private void AddRow(TableLayoutPanel layout, int rowIndex, string labelText, Control editor, Control extra)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 12, 6),
                Text = labelText
            };

            editor.Margin = new Padding(0, 3, 0, 3);
            editor.Dock = DockStyle.Fill;

            layout.Controls.Add(label, 0, rowIndex);
            layout.Controls.Add(editor, 1, rowIndex);
            if (extra != null)
            {
                extra.Margin = new Padding(8, 3, 0, 3);
                layout.Controls.Add(extra, 2, rowIndex);
            }
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select media folder";
                if (Directory.Exists(_mediaFolderTextBox.Text))
                {
                    dialog.SelectedPath = _mediaFolderTextBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _mediaFolderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            var settings = new EditableSettings(
                _mediaFolderTextBox.Text.Trim(),
                (int)_intervalNumeric.Value,
                (int)_scanIntervalNumeric.Value,
                (int)_scanTimeoutNumeric.Value,
                GetSelectedScreenMode(),
                _selectedScreenIndex);
            var errors = SettingsValidator.Validate(settings);
            if (errors.Count > 0)
            {
                MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_saveHandler(settings))
            {
                _dirty = false;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void OnExitApplicationClick(object sender, EventArgs e)
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            if (_exitHandler())
            {
                ExitApplicationRequested = true;
                DialogResult = DialogResult.Abort;
                Close();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK || DialogResult == DialogResult.Abort)
            {
                return;
            }

            if (!ConfirmDiscardIfDirty())
            {
                e.Cancel = true;
            }
        }

        private bool ConfirmDiscardIfDirty()
        {
            if (!_dirty)
            {
                return true;
            }

            return MessageBox.Show(
                       this,
                       "Discard unsaved changes?",
                       "Settings",
                       MessageBoxButtons.YesNo,
                       MessageBoxIcon.Question) == DialogResult.Yes;
        }

        internal void SelectScreenMode(string screenMode)
        {
            var canonicalMode = DisplayModeParser.Canonicalize(screenMode);
            for (var index = 0; index < _screenModeComboBox.Items.Count; index++)
            {
                var option = (ScreenModeOption)_screenModeComboBox.Items[index];
                if (string.Equals(option.Value, canonicalMode, StringComparison.Ordinal))
                {
                    _screenModeComboBox.SelectedIndex = index;
                    return;
                }
            }

            _screenModeComboBox.SelectedIndex = 0;
        }

        internal void SelectScreenIndex(int screenIndex)
        {
            _selectedScreenIndex = screenIndex;
            for (var index = 0; index < _screenComboBox.Items.Count; index++)
            {
                var screen = (DisplayScreen)_screenComboBox.Items[index];
                if (screen.Index == screenIndex)
                {
                    _screenComboBox.SelectedIndex = index;
                    return;
                }
            }

            if (_screenComboBox.Items.Count > 0)
            {
                _screenComboBox.SelectedIndex = 0;
                _selectedScreenIndex = ((DisplayScreen)_screenComboBox.SelectedItem).Index;
            }
        }

        internal void PerformSave()
        {
            OnSaveClick(this, EventArgs.Empty);
        }

        private string GetSelectedScreenMode()
        {
            var option = _screenModeComboBox.SelectedItem as ScreenModeOption;
            return option == null ? DisplayModeParser.AllScreens : option.Value;
        }

        private void ApplyScreenSelectorState()
        {
            _screenComboBox.Enabled = _screens.Count > 0 &&
                                      string.Equals(GetSelectedScreenMode(), DisplayModeParser.SingleScreen, StringComparison.Ordinal);
        }

        private void MarkDirty()
        {
            if (!_initializing)
            {
                _dirty = true;
            }
        }

        private sealed class ScreenModeOption
        {
            public ScreenModeOption(string text, string value)
            {
                Text = text;
                Value = value;
            }

            public string Text { get; private set; }
            public string Value { get; private set; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
