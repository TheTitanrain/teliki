using System;
using System.IO;

namespace Teliki.Core
{
    public static class AppConfigNormalizer
    {
        public static AppConfig Normalize(AppConfig config, string baseDirectory)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            return new AppConfig(
                NormalizePath(config.MediaFolder, baseDirectory),
                config.Interval,
                config.ScanInterval,
                config.ScanTimeout,
                NormalizePath(config.CacheFolder, baseDirectory),
                config.MaxCacheSizeMb,
                config.MinFreeDiskMb,
                config.ScreenMode,
                config.ScreenIndex,
                config.Muted);
        }

        private static string NormalizePath(string path, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(baseDirectory, path));
        }
    }
}
