using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class SettingsForm : Form
    {
        private readonly Func<EditableSettings, bool> _saveHandler;
        private readonly Func<bool> _exitHandler;
        private readonly TextBox _mediaFolderTextBox = new TextBox();
        private readonly NumericUpDown _intervalNumeric = new NumericUpDown();
        private readonly NumericUpDown _scanIntervalNumeric = new NumericUpDown();
        private readonly NumericUpDown _scanTimeoutNumeric = new NumericUpDown();
        private readonly Button _saveButton = new Button();
        private readonly Button _cancelButton = new Button();
        private readonly Button _exitButton = new Button();
        private readonly Button _browseButton = new Button();
        private readonly IDisposable _cursorScope;
        private bool _dirty;

        public SettingsForm(EditableSettings settings, Func<EditableSettings, bool> saveHandler, Func<bool> exitHandler)
        {
            _saveHandler = saveHandler;
            _exitHandler = exitHandler;

            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            Width = 520;
            Height = 260;
            _cursorScope = CursorVisibilityManager.Shared.ShowCursorWhileModalUiOpen();

            ConfigureControls(settings);
            Controls.Add(BuildLayout());

            CancelButton = _cancelButton;
            FormClosing += OnFormClosing;
        }

        public bool ExitApplicationRequested { get; private set; }

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
            _mediaFolderTextBox.Text = settings.MediaFolder;
            _mediaFolderTextBox.TextChanged += delegate { _dirty = true; };

            ConfigureNumeric(_intervalNumeric, settings.IntervalSeconds);
            ConfigureNumeric(_scanIntervalNumeric, settings.ScanIntervalSeconds);
            ConfigureNumeric(_scanTimeoutNumeric, settings.ScanTimeoutSeconds);

            _browseButton.Text = "Browse...";
            _browseButton.Click += OnBrowseClick;

            _saveButton.Text = "Save";
            _saveButton.Click += OnSaveClick;

            _cancelButton.Text = "Cancel";
            _cancelButton.Click += delegate { Close(); };

            _exitButton.Text = "Exit Application";
            _exitButton.Click += OnExitApplicationClick;
        }

        private void ConfigureNumeric(NumericUpDown control, int value)
        {
            control.Minimum = 1;
            control.Maximum = 86400;
            control.Value = Math.Max(1, value);
            control.Width = 120;
            control.ValueChanged += delegate { _dirty = true; };
        }

        private Control BuildLayout()
        {
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(12);
            layout.ColumnCount = 3;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            AddRow(layout, 0, "Media Folder", _mediaFolderTextBox, _browseButton);
            AddRow(layout, 1, "Interval (sec)", _intervalNumeric, null);
            AddRow(layout, 2, "Scan Interval (sec)", _scanIntervalNumeric, null);
            AddRow(layout, 3, "Scan Timeout (sec)", _scanTimeoutNumeric, null);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Controls.Add(_saveButton);
            buttons.Controls.Add(_cancelButton);
            buttons.Controls.Add(_exitButton);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 3);

            return layout;
        }

        private void AddRow(TableLayoutPanel layout, int rowIndex, string labelText, Control editor, Control extra)
        {
            layout.Controls.Add(new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 8, 6)
            }, 0, rowIndex);
            editor.Dock = DockStyle.Fill;
            layout.Controls.Add(editor, 1, rowIndex);
            if (extra != null)
            {
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
                (int)_scanTimeoutNumeric.Value);
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
    }
}
