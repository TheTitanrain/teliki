using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class DisplayForm : Form, IMediaRenderer
    {
        private readonly IDisplayCommandTarget _commandTarget;
        private readonly DisplayScreen _screen;
        private readonly ILogger _logger;
        private readonly PictureBox _pictureBox = new PictureBox();
        private readonly WmpHost _wmpHost = new WmpHost();
        private Image _currentImage;
        private MemoryStream _currentImageStream;

        public DisplayForm(DisplayScreen screen, ILogger logger, IDisplayCommandTarget commandTarget)
        {
            _commandTarget = commandTarget;
            _screen = screen;
            _logger = logger;

            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            TopMost = true;
            BackColor = Color.Black;
            CursorVisibilityManager.Shared.HideForPlayback();
            _pictureBox.Dock = DockStyle.Fill;
            _pictureBox.BackColor = Color.Black;
            _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            _wmpHost.Dock = DockStyle.Fill;
            _wmpHost.Visible = false;

            Controls.Add(_pictureBox);
            Controls.Add(_wmpHost);

            Deactivate += delegate { RestoreFullscreen(); };
            Resize += delegate { RestoreFullscreen(); };
            Move += delegate { RestoreFullscreen(); };
        }

        public void RestoreFullscreen()
        {
            if (IsDisposed)
            {
                return;
            }

            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
            Bounds = new Rectangle(_screen.X, _screen.Y, _screen.Width, _screen.Height);
            TopMost = false;
            TopMost = true;
            BringToFront();
        }

        public void Render(CachedMediaItem item)
        {
            if (item.Kind == MediaKind.Image)
            {
                RenderImage(item.CachedPath);
            }
            else
            {
                RenderVideo(item.CachedPath);
            }
        }

        public void ShowBlank()
        {
            StopVideo();
            SetImage(null, null);
            _pictureBox.Visible = true;
            _wmpHost.Visible = false;
            BackColor = Color.Black;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopVideo();
                SetImage(null, null);
                _pictureBox.Dispose();
                _wmpHost.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_commandTarget != null && _commandTarget.ArePlaybackHotkeysEnabled)
            {
                if (keyData == Keys.F1)
                {
                    _commandTarget.OpenAbout(this);
                    return true;
                }

                if (keyData == Keys.F2)
                {
                    _commandTarget.OpenSettings(this);
                    return true;
                }

                if (keyData == Keys.Escape)
                {
                    _commandTarget.ExitApplication();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RenderImage(string path)
        {
            StopVideo();
            _wmpHost.Visible = false;
            _pictureBox.Visible = true;

            try
            {
                var extension = Path.GetExtension(path);
                if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = File.ReadAllBytes(path);
                    var stream = new MemoryStream(bytes);
                    SetImage(Image.FromStream(stream), stream);
                }
                else
                {
                    using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var image = Image.FromStream(stream))
                    {
                        SetImage(new Bitmap(image), null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to render image: " + path, ex);
                ShowBlank();
            }
        }

        private void RenderVideo(string path)
        {
            SetImage(null, null);
            _pictureBox.Visible = false;
            _wmpHost.Visible = true;
            _wmpHost.BringToFront();

            try
            {
                _wmpHost.Play(path);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to render video: " + path, ex);
                ShowBlank();
            }
        }

        private void StopVideo()
        {
            try
            {
                _wmpHost.Stop();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to stop video.", ex);
            }
        }

        private void SetImage(Image image, MemoryStream backingStream)
        {
            var previousImage = _currentImage;
            var previousStream = _currentImageStream;

            _currentImage = image;
            _currentImageStream = backingStream;
            _pictureBox.Image = image;

            if (previousImage != null)
            {
                previousImage.Dispose();
            }

            if (previousStream != null)
            {
                previousStream.Dispose();
            }
        }
    }

    internal interface IDisplayCommandTarget
    {
        bool ArePlaybackHotkeysEnabled { get; }
        void OpenSettings(DisplayForm owner);
        void OpenAbout(DisplayForm owner);
        void ExitApplication();
    }

    internal sealed class WmpHost : AxHost
    {
        public WmpHost()
            : base("6BF52A52-394A-11d3-B153-00C04F79FAA6")
        {
        }

        public void Play(string path)
        {
            CreateControl();
            var ocx = GetOcx();
            SetProperty(ocx, "uiMode", "none");
            SetProperty(ocx, "stretchToFit", true);
            var settings = GetProperty(ocx, "settings");
            if (settings != null)
            {
                SetProperty(settings, "autoStart", true);
            }

            SetProperty(ocx, "URL", path);
        }

        public void Stop()
        {
            var ocx = GetOcx();
            if (ocx == null)
            {
                return;
            }

            var controls = GetProperty(ocx, "controls");
            if (controls != null)
            {
                controls.GetType().InvokeMember("stop", BindingFlags.InvokeMethod, null, controls, null);
            }
        }

        private static object GetProperty(object target, string property)
        {
            if (target == null)
            {
                return null;
            }

            return target.GetType().InvokeMember(property, BindingFlags.GetProperty, null, target, null);
        }

        private static void SetProperty(object target, string property, object value)
        {
            if (target == null)
            {
                return;
            }

            target.GetType().InvokeMember(property, BindingFlags.SetProperty, null, target, new[] { value });
        }
    }
}
