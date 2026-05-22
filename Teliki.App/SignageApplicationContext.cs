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
        private readonly IScreenProvider _screenProvider;
        private readonly Control _uiDispatcher = new Control();
        private readonly List<DisplayForm> _forms = new List<DisplayForm>();
        private readonly Timer _advanceTimer = new Timer();
        private readonly Timer _scanTimer = new Timer();
        private readonly Timer _watchdogTimer = new Timer();
        private readonly BackgroundScanRunner _scanRunner;
        private readonly SignageController _controller;
        private readonly ApplicationShutdownCoordinator _shutdownCoordinator = new ApplicationShutdownCoordinator();
        private DisplayCoordinator _displayCoordinator;
        private CachedMediaItem _currentItemSnapshot;
        private bool _rebuildingDisplays;
        private bool _pendingDisplayRefresh;

        public SignageApplicationContext(AppConfig config, ILogger logger, string configPath, string baseDirectory)
        {
            _logger = logger;
            _playlist = new PlaylistService();
            _screenProvider = new WindowsScreenProvider();
            var ignore = _uiDispatcher.Handle;
            var scanner = new MediaScanner(new PhysicalFileSystem(), logger);
            var cache = new MediaCache(new PhysicalFileSystem(), logger);
            _scanRunner = new BackgroundScanRunner(scanner, cache, logger);
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
            RebuildDisplayForms(config, false);

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
                _uiDispatcher.Dispose();
                foreach (var form in _forms)
                {
                    form.FormClosed -= OnFormClosed;
                    form.VideoPlaybackCompleted -= OnVideoPlaybackCompleted;
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
            if (_uiDispatcher.IsDisposed || !_uiDispatcher.IsHandleCreated)
            {
                return;
            }

            _uiDispatcher.BeginInvoke(action);
        }

        private void OnScanCompleted(ScanCompletion completion)
        {
            BeginInvokeOnUi(delegate { _controller.OnScanCompleted(completion); });
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            BeginInvokeOnUi(HandleDisplaySettingsChanged);
        }

        public void ApplyPlaylist(PlaylistManifest manifest)
        {
            _currentItemSnapshot = null;
            _playlist.Replace(manifest);
        }

        public void AdvancePlayback()
        {
            _currentItemSnapshot = _displayCoordinator.Advance();
            if (_currentItemSnapshot != null && _currentItemSnapshot.Kind == MediaKind.Video)
            {
                _controller.PauseAdvanceForVideo();
            }
            else
            {
                _controller.SetNextInterval(_currentItemSnapshot?.Duration);
            }
        }

        private void OnVideoPlaybackCompleted(object sender, EventArgs e)
        {
            // Called on UI thread (WmpHost uses WinForms Timer)
            _controller.OnVideoCompleted();
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

            var displayRebuildRequested = false;
            try
            {
                var availableScreens = _screenProvider.GetScreens();
                var initialSettings = NormalizeSettingsForForm(_controller.LoadEditableSettings(), availableScreens);
                using (var form = new SettingsForm(
                           initialSettings,
                           availableScreens,
                           delegate(EditableSettings settings)
                           {
                               try
                               {
                                   var previousConfig = _controller.CurrentConfig;
                                   _controller.SaveSettings(settings);
                                   if (HasDisplaySettingsChanged(previousConfig, _controller.CurrentConfig))
                                   {
                                       displayRebuildRequested = true;
                                   }

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
                var shouldRebuild = displayRebuildRequested || _pendingDisplayRefresh;
                _controller.CloseModalUi(!shouldRebuild);
                if (shouldRebuild)
                {
                    _pendingDisplayRefresh = false;
                    RebuildDisplayForms(_controller.CurrentConfig, true);
                }
                else
                {
                    ApplyMuteToForms(_controller.CurrentConfig.Muted);
                }
            }
        }

        public void OpenAbout(DisplayForm owner)
        {
            if (!_controller.TryOpenAbout())
                return;

            try
            {
                using (var form = new AboutForm())
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
            if (_rebuildingDisplays)
            {
                return;
            }

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

        private void HandleDisplaySettingsChanged()
        {
            if (!ArePlaybackHotkeysEnabled)
            {
                _pendingDisplayRefresh = true;
                return;
            }

            RebuildDisplayForms(_controller.CurrentConfig, true);
        }

        private void RebuildDisplayForms(AppConfig config, bool renderCurrentItem)
        {
            var selection = DisplayScreenSelector.SelectScreens(config, _screenProvider.GetScreens());
            if (selection.UsedFallback && !string.IsNullOrEmpty(selection.Warning))
            {
                _logger.Warn(selection.Warning);
            }

            var previousForms = _forms.ToArray();
            var nextForms = new List<DisplayForm>();
            foreach (var screen in selection.Screens)
            {
                var form = new DisplayForm(screen, _logger, this);
                form.SetMuted(config.Muted);
                form.FormClosed += OnFormClosed;
                form.VideoPlaybackCompleted += OnVideoPlaybackCompleted;
                nextForms.Add(form);
            }

            foreach (var form in nextForms)
            {
                form.Show();
                form.RestoreFullscreen();
            }

            _forms.Clear();
            _forms.AddRange(nextForms);
            _displayCoordinator = new DisplayCoordinator(_playlist, nextForms.ToArray(), _logger);
            if (renderCurrentItem)
            {
                _displayCoordinator.Render(_currentItemSnapshot);
            }

            _rebuildingDisplays = true;
            try
            {
                foreach (var form in previousForms)
                {
                    form.FormClosed -= OnFormClosed;
                    form.VideoPlaybackCompleted -= OnVideoPlaybackCompleted;
                    if (!form.IsDisposed)
                    {
                        form.Close();
                    }
                    else
                    {
                        form.Dispose();
                    }
                }
            }
            finally
            {
                _rebuildingDisplays = false;
            }
        }

        private void ApplyMuteToForms(bool muted)
        {
            foreach (var form in _forms)
            {
                if (!form.IsDisposed)
                {
                    form.SetMuted(muted);
                }
            }
        }

        private static bool HasDisplaySettingsChanged(AppConfig previousConfig, AppConfig nextConfig)
        {
            return previousConfig.DisplayMode != nextConfig.DisplayMode ||
                   previousConfig.ScreenIndex != nextConfig.ScreenIndex;
        }

        private static EditableSettings NormalizeSettingsForForm(EditableSettings settings, IReadOnlyList<DisplayScreen> screens)
        {
            var screenIndex = settings.ScreenIndex;
            if (screens.Count > 0)
            {
                if (screenIndex < 1)
                {
                    var fallback = screens[0];
                    foreach (var screen in screens)
                    {
                        if (screen.Primary)
                        {
                            fallback = screen;
                            break;
                        }
                    }

                    screenIndex = fallback.Index;
                }

                if (DisplayModeParser.Parse(settings.ScreenMode) == DisplayTargetMode.SingleScreen)
                {
                    var config = new AppConfig(
                        settings.MediaFolder,
                        TimeSpan.FromSeconds(settings.IntervalSeconds),
                        TimeSpan.FromSeconds(settings.ScanIntervalSeconds),
                        TimeSpan.FromSeconds(settings.ScanTimeoutSeconds),
                        string.Empty,
                        1,
                        0,
                        settings.ScreenMode,
                        screenIndex);
                    var selection = DisplayScreenSelector.SelectScreens(config, screens);
                    if (selection.Screens.Count > 0)
                    {
                        screenIndex = selection.Screens[0].Index;
                    }
                }
            }

            return new EditableSettings(
                settings.MediaFolder,
                settings.IntervalSeconds,
                settings.ScanIntervalSeconds,
                settings.ScanTimeoutSeconds,
                DisplayModeParser.Canonicalize(settings.ScreenMode),
                screenIndex,
                settings.Muted);
        }
    }
}
