using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Teliki.App
{
    internal sealed class AboutForm : Form
    {
        private readonly IDisposable _cursorScope;

        public AboutForm()
        {
            Text = "О программе";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(360, 185);
            _cursorScope = CursorVisibilityManager.Shared.ShowCursorWhileModalUiOpen();

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var versionText = version != null
                ? string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build)
                : "–";

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label
            {
                Text = "Teliki",
                Font = new Font(Font.FontFamily, Font.Size + 4, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            }, 0, 0);

            layout.Controls.Add(new Label
            {
                Text = "Версия " + versionText,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            }, 0, 1);

            layout.Controls.Add(new Label
            {
                Text = "© " + DateTime.Now.Year,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            }, 0, 2);

            var siteLink = new LinkLabel
            {
                Text = "thetitanrain.github.io/teliki",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 16)
            };
            siteLink.LinkClicked += (s, e) =>
                Process.Start(new ProcessStartInfo("https://thetitanrain.github.io/teliki/") { UseShellExecute = true });
            layout.Controls.Add(siteLink, 0, 3);

            var okButton = new Button
            {
                Text = "ОК",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(80, 0),
                Anchor = AnchorStyles.None
            };
            layout.Controls.Add(okButton, 0, 4);
            Controls.Add(layout);
            AcceptButton = okButton;
            CancelButton = okButton;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _cursorScope.Dispose();
            base.Dispose(disposing);
        }
    }
}
