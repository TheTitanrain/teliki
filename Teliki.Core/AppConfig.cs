using System;

namespace Teliki.Core
{
    public sealed class AppConfig
    {
        public string MediaFolder { get; private set; }
        public TimeSpan Interval { get; private set; }
        public TimeSpan ScanInterval { get; private set; }
        public TimeSpan ScanTimeout { get; private set; }
        public string CacheFolder { get; private set; }
        public int MaxCacheSizeMb { get; private set; }
        public int MinFreeDiskMb { get; private set; }
        public string ScreenMode { get; private set; }
        public int ScreenIndex { get; private set; }
        public DisplayTargetMode DisplayMode { get; private set; }

        public AppConfig(
            string mediaFolder,
            TimeSpan interval,
            TimeSpan scanInterval,
            TimeSpan scanTimeout,
            string cacheFolder,
            int maxCacheSizeMb,
            int minFreeDiskMb,
            string screenMode,
            int screenIndex)
        {
            MediaFolder = mediaFolder;
            Interval = interval;
            ScanInterval = scanInterval;
            ScanTimeout = scanTimeout;
            CacheFolder = cacheFolder;
            MaxCacheSizeMb = maxCacheSizeMb;
            MinFreeDiskMb = minFreeDiskMb;
            ScreenMode = screenMode;
            ScreenIndex = screenIndex;
            DisplayMode = DisplayModeParser.Parse(screenMode);
        }
    }
}
