using System;
using Teliki.Core;

namespace Teliki.App
{
    public interface IAppTimer
    {
        int Interval { get; set; }
        void Start();
        void Stop();
    }

    public interface ISignageRuntime
    {
        void ApplyPlaylist(PlaylistManifest manifest);
        void AdvancePlayback();
        void RestoreFullscreen();
        void ExitApplication();
    }

    public interface IScanRunner
    {
        event Action<ScanCompletion> ScanCompleted;
        void Start(ScanRequest request);
    }

    public sealed class ScanRequest
    {
        public ScanRequest(AppConfig config, long generation)
        {
            Config = config;
            Generation = generation;
        }

        public AppConfig Config { get; private set; }
        public long Generation { get; private set; }
    }

    public sealed class ScanCompletion
    {
        public ScanCompletion(long generation, PlaylistManifest manifest)
        {
            Generation = generation;
            Manifest = manifest;
        }

        public long Generation { get; private set; }
        public PlaylistManifest Manifest { get; private set; }
    }

    public sealed class SignageController
    {
        private readonly ISignageRuntime _runtime;
        private readonly IScanRunner _scanRunner;
        private readonly ILogger _logger;
        private readonly ConfigFileStore _configStore;
        private readonly string _configPath;
        private readonly string _baseDirectory;
        private readonly Func<DateTime> _utcNow;
        private AppConfig _currentConfig;
        private bool _modalUiOpen;
        private bool _settingsOpen;
        private bool _scanRunning;
        private bool _pendingRescan;
        private bool _displayingContent;
        private long _configGeneration;
        private DateTime _scanStartedUtc;

        public SignageController(
            AppConfig config,
            ISignageRuntime runtime,
            IScanRunner scanRunner,
            IAppTimer advanceTimer,
            IAppTimer scanTimer,
            IAppTimer watchdogTimer,
            ILogger logger,
            ConfigFileStore configStore = null,
            string configPath = null,
            string baseDirectory = null,
            Func<DateTime> utcNow = null)
        {
            _currentConfig = config;
            _runtime = runtime;
            _scanRunner = scanRunner;
            AdvanceTimer = advanceTimer;
            ScanTimer = scanTimer;
            WatchdogTimer = watchdogTimer;
            _logger = logger;
            _configStore = configStore;
            _configPath = configPath;
            _baseDirectory = baseDirectory;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);

            UpdateTimerIntervals(config);
        }

        public IAppTimer AdvanceTimer { get; private set; }
        public IAppTimer ScanTimer { get; private set; }
        public IAppTimer WatchdogTimer { get; private set; }
        public bool ArePlaybackHotkeysEnabled { get { return !_modalUiOpen; } }
        public AppConfig CurrentConfig { get { return _currentConfig; } }

        public void StartRuntime()
        {
            AdvanceTimer.Start();
            ScanTimer.Start();
            WatchdogTimer.Start();
            RequestScan();
        }

        public EditableSettings LoadEditableSettings()
        {
            if (_configStore == null || string.IsNullOrEmpty(_configPath))
            {
                return new EditableSettings(
                    _currentConfig.MediaFolder,
                    (int)_currentConfig.Interval.TotalSeconds,
                    (int)_currentConfig.ScanInterval.TotalSeconds,
                    (int)_currentConfig.ScanTimeout.TotalSeconds,
                    _currentConfig.ScreenMode,
                    _currentConfig.ScreenIndex);
            }

            return _configStore.Load(_configPath).GetEditableSettings();
        }

        public void SaveSettings(EditableSettings settings)
        {
            if (_configStore == null || string.IsNullOrEmpty(_configPath) || string.IsNullOrEmpty(_baseDirectory))
            {
                throw new InvalidOperationException("Settings persistence is not configured.");
            }

            var errors = SettingsValidator.Validate(settings);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
            }

            var document = _configStore.Load(_configPath);
            document.SetEditableSettings(settings);
            _configStore.Save(_configPath, document);

            var reloaded = ConfigLoader.Load(_configPath);
            ApplyConfig(AppConfigNormalizer.Normalize(reloaded, _baseDirectory));
        }

        public void ApplyConfig(AppConfig newConfig)
        {
            AdvanceTimer.Stop();
            ScanTimer.Stop();
            WatchdogTimer.Stop();

            _currentConfig = newConfig;
            _configGeneration++;
            UpdateTimerIntervals(newConfig);

            AdvanceTimer.Start();
            ScanTimer.Start();
            WatchdogTimer.Start();

            if (_scanRunning)
            {
                _pendingRescan = true;
                return;
            }

            RequestScan();
        }

        public void RequestScan()
        {
            if (_scanRunning)
            {
                return;
            }

            _scanRunning = true;
            _scanStartedUtc = _utcNow();
            _scanRunner.Start(new ScanRequest(_currentConfig, _configGeneration));
        }

        public void OnScanCompleted(ScanCompletion completion)
        {
            _scanRunning = false;
            if (completion.Generation == _configGeneration)
            {
                var needsImmediateAdvance = !_displayingContent && completion.Manifest.Items.Count > 0;
                _runtime.ApplyPlaylist(completion.Manifest);
                _displayingContent = completion.Manifest.Items.Count > 0;
                if (needsImmediateAdvance)
                {
                    _runtime.AdvancePlayback();
                }
            }
            else
            {
                _logger.Warn("Ignoring stale scan result from an older config generation.");
            }

            if (_pendingRescan)
            {
                _pendingRescan = false;
                RequestScan();
            }
        }

        public bool TryOpenSettings()
        {
            if (_settingsOpen)
            {
                return false;
            }

            _settingsOpen = true;
            _modalUiOpen = true;
            return true;
        }

        public void CloseModalUi(bool restoreFullscreen = true)
        {
            _settingsOpen = false;
            _modalUiOpen = false;
            if (restoreFullscreen)
            {
                _runtime.RestoreFullscreen();
            }
        }

        public void OnAdvanceTick()
        {
            _runtime.AdvancePlayback();
        }

        public void OnScanTick()
        {
            if (_scanRunning)
            {
                if (_utcNow() - _scanStartedUtc > _currentConfig.ScanTimeout)
                {
                    _logger.Warn("Media scan exceeded timeout. Keeping current playlist and skipping overlapping scan.");
                }

                return;
            }

            RequestScan();
        }

        public void OnWatchdogTick()
        {
            if (!_modalUiOpen)
            {
                _runtime.RestoreFullscreen();
            }
        }

        public void HandlePlaybackEscape()
        {
            if (_modalUiOpen)
            {
                return;
            }

            ExitApplication();
        }

        public void ExitApplication()
        {
            _runtime.ExitApplication();
        }

        private void UpdateTimerIntervals(AppConfig config)
        {
            AdvanceTimer.Interval = ToTimerInterval(config.Interval);
            ScanTimer.Interval = ToTimerInterval(config.ScanInterval);
            WatchdogTimer.Interval = 1000;
        }

        private static int ToTimerInterval(TimeSpan value)
        {
            return Math.Max(1, (int)Math.Min(int.MaxValue, value.TotalMilliseconds));
        }
    }
}
