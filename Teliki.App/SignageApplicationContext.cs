using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class SignageApplicationContext : ApplicationContext
    {
        private readonly AppConfig _config;
        private readonly ILogger _logger;
        private readonly MediaScanner _scanner;
        private readonly MediaCache _cache;
        private readonly PlaylistService _playlist;
        private readonly DisplayCoordinator _displayCoordinator;
        private readonly List<DisplayForm> _forms = new List<DisplayForm>();
        private readonly Timer _advanceTimer = new Timer();
        private readonly Timer _scanTimer = new Timer();
        private readonly Timer _watchdogTimer = new Timer();
        private readonly object _scanSync = new object();
        private DateTime _scanStartedUtc;
        private bool _scanRunning;

        public SignageApplicationContext(AppConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
            _scanner = new MediaScanner(new PhysicalFileSystem(), logger);
            _cache = new MediaCache(new PhysicalFileSystem(), logger);
            _playlist = new PlaylistService();

            var screenProvider = new WindowsScreenProvider();
            foreach (var screen in screenProvider.GetScreens())
            {
                var form = new DisplayForm(screen, logger);
                form.FormClosed += OnFormClosed;
                _forms.Add(form);
            }

            _displayCoordinator = new DisplayCoordinator(_playlist, _forms.ToArray(), logger);
            ConfigureTimers();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            foreach (var form in _forms)
            {
                form.Show();
                form.RestoreFullscreen();
            }

            StartScanIfPossible();
            _advanceTimer.Start();
            _scanTimer.Start();
            _watchdogTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                _advanceTimer.Dispose();
                _scanTimer.Dispose();
                _watchdogTimer.Dispose();
                foreach (var form in _forms)
                {
                    form.FormClosed -= OnFormClosed;
                    form.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void ConfigureTimers()
        {
            _advanceTimer.Interval = ToTimerInterval(_config.Interval);
            _advanceTimer.Tick += delegate { _displayCoordinator.Advance(); };

            _scanTimer.Interval = ToTimerInterval(_config.ScanInterval);
            _scanTimer.Tick += delegate { StartScanIfPossible(); };

            _watchdogTimer.Interval = 1000;
            _watchdogTimer.Tick += delegate { RestoreAllForms(); };
        }

        private static int ToTimerInterval(TimeSpan value)
        {
            return Math.Max(1, (int)Math.Min(int.MaxValue, value.TotalMilliseconds));
        }

        private void StartScanIfPossible()
        {
            lock (_scanSync)
            {
                if (_scanRunning)
                {
                    if (DateTime.UtcNow - _scanStartedUtc > _config.ScanTimeout)
                    {
                        _logger.Warn("Media scan exceeded timeout. Keeping current playlist and skipping overlapping scan.");
                    }

                    return;
                }

                _scanRunning = true;
                _scanStartedUtc = DateTime.UtcNow;
            }

            Task.Run(delegate
            {
                try
                {
                    var result = _scanner.Scan(_config.MediaFolder);
                    var manifest = _cache.Promote(
                        result,
                        _config.CacheFolder,
                        CacheSettings.FromMegabytes(_config.MaxCacheSizeMb, _config.MinFreeDiskMb));
                    BeginInvokeOnUi(delegate
                    {
                        _playlist.Replace(manifest);
                        _displayCoordinator.Advance();
                    });
                }
                catch (Exception ex)
                {
                    _logger.Error("Background scan failed.", ex);
                }
                finally
                {
                    lock (_scanSync)
                    {
                        _scanRunning = false;
                    }
                }
            });
        }

        private void BeginInvokeOnUi(Action action)
        {
            if (_forms.Count == 0 || _forms[0].IsDisposed)
            {
                return;
            }

            _forms[0].BeginInvoke(action);
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            RestoreAllForms();
        }

        private void RestoreAllForms()
        {
            foreach (var form in _forms)
            {
                if (!form.IsDisposed)
                {
                    form.RestoreFullscreen();
                }
            }
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            var allClosed = true;
            foreach (var form in _forms)
            {
                allClosed &= form.IsDisposed;
            }

            if (allClosed)
            {
                ExitThread();
            }
        }
    }
}
