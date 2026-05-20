using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class SignageApplicationContext : ApplicationContext, ISignageRuntime, IDisplayCommandTarget
    {
        private readonly ILogger _logger;
        private readonly PlaylistService _playlist;
        private readonly DisplayCoordinator _displayCoordinator;
        private readonly List<DisplayForm> _forms = new List<DisplayForm>();
        private readonly Timer _advanceTimer = new Timer();
        private readonly Timer _scanTimer = new Timer();
        private readonly Timer _watchdogTimer = new Timer();
        private readonly BackgroundScanRunner _scanRunner;
        private readonly SignageController _controller;
        private readonly ApplicationShutdownCoordinator _shutdownCoordinator = new ApplicationShutdownCoordinator();

        public SignageApplicationContext(AppConfig config, ILogger logger, string configPath, string baseDirectory)
        {
            _logger = logger;
            _playlist = new PlaylistService();
            var scanner = new MediaScanner(new PhysicalFileSystem(), logger);
            var cache = new MediaCache(new PhysicalFileSystem(), logger);
            _scanRunner = new BackgroundScanRunner(scanner, cache, logger);

            var screenProvider = new WindowsScreenProvider();
            foreach (var screen in screenProvider.GetScreens())
            {
                var form = new DisplayForm(screen, logger, this);
                form.FormClosed += OnFormClosed;
                _forms.Add(form);
            }

            _displayCoordinator = new DisplayCoordinator(_playlist, _forms.ToArray(), logger);
            _controller = new SignageController(
                config,
                this,
                _scanRunner,
                new WinFormsTimerAdapter(_advanceTimer),
                new WinFormsTimerAdapter(_scanTimer),
                new WinFormsTimerAdapter(_watchdogTimer),
                logger,
                new ConfigFileStore(new PhysicalFileSystem()),
                configPath,
                baseDirectory);
            ConfigureTimers();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            _scanRunner.ScanCompleted += OnScanCompleted;

            foreach (var form in _forms)
            {
                form.Show();
                form.RestoreFullscreen();
            }

            _controller.StartRuntime();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                _scanRunner.ScanCompleted -= OnScanCompleted;
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
            _advanceTimer.Tick += delegate { _controller.OnAdvanceTick(); };
            _scanTimer.Tick += delegate { _controller.OnScanTick(); };
            _watchdogTimer.Tick += delegate { _controller.OnWatchdogTick(); };
        }

        private void BeginInvokeOnUi(Action action)
        {
            if (_forms.Count == 0 || _forms[0].IsDisposed)
            {
                return;
            }

            _forms[0].BeginInvoke(action);
        }

        private void OnScanCompleted(ScanCompletion completion)
        {
            BeginInvokeOnUi(delegate { _controller.OnScanCompleted(completion); });
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            _controller.OnWatchdogTick();
        }

        public void ApplyPlaylist(PlaylistManifest manifest)
        {
            _playlist.Replace(manifest);
        }

        public void AdvancePlayback()
        {
            _displayCoordinator.Advance();
        }

        public void RestoreFullscreen()
        {
            foreach (var form in _forms)
            {
                if (!form.IsDisposed)
                {
                    form.RestoreFullscreen();
                }
            }
        }

        public void ExitApplication()
        {
            if (!_shutdownCoordinator.RequestExit())
            {
                return;
            }

            foreach (var form in _forms)
            {
                if (!form.IsDisposed)
                {
                    form.Close();
                }
            }

            ExitThread();
        }

        public bool ArePlaybackHotkeysEnabled
        {
            get { return _controller.ArePlaybackHotkeysEnabled; }
        }

        public void OpenSettings(DisplayForm owner)
        {
            if (!_controller.TryOpenSettings())
            {
                return;
            }

            try
            {
                using (var form = new SettingsForm(
                           _controller.LoadEditableSettings(),
                           delegate(EditableSettings settings)
                           {
                               try
                               {
                                   _controller.SaveSettings(settings);
                                   return true;
                               }
                               catch (Exception ex)
                               {
                                   _logger.Error("Failed to save settings.", ex);
                                   MessageBox.Show(owner, ex.Message, "Save failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                   return false;
                               }
                           },
                           delegate
                            {
                                _controller.ExitApplication();
                                return true;
                            }))
                {
                    form.ShowDialog(owner);
                }
            }
            finally
            {
                _controller.CloseModalUi();
            }
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            var remainingOpenForms = 0;
            foreach (var form in _forms)
            {
                if (!ReferenceEquals(form, sender) && !form.IsDisposed)
                {
                    remainingOpenForms++;
                }
            }

            if (_shutdownCoordinator.ShouldExitThreadAfterFormClosed(remainingOpenForms))
            {
                ExitThread();
            }
        }
    }
}
