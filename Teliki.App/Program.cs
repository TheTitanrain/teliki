using System;
using System.IO;
using System.Windows.Forms;
using Teliki.Core;

namespace Teliki.App
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(baseDirectory, "appsettings.ini");
            var config = ConfigLoader.Load(configPath);
            config = NormalizePaths(config, baseDirectory);

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Teliki",
                "logs",
                "teliki.log");
            var logger = new FileLogger(logPath);

            Application.Run(new SignageApplicationContext(config, logger));
        }

        private static AppConfig NormalizePaths(AppConfig config, string baseDirectory)
        {
            return new AppConfig(
                NormalizePath(config.MediaFolder, baseDirectory),
                config.Interval,
                config.ScanInterval,
                config.ScanTimeout,
                NormalizePath(config.CacheFolder, baseDirectory),
                config.MaxCacheSizeMb,
                config.MinFreeDiskMb,
                config.ScreenMode);
        }

        private static string NormalizePath(string path, string baseDirectory)
        {
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseDirectory, path));
        }
    }
}
