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
            config = AppConfigNormalizer.Normalize(config, baseDirectory);

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Teliki",
                "logs",
                "teliki.log");
            var logger = new FileLogger(logPath);

            Application.Run(new SignageApplicationContext(config, logger, configPath, baseDirectory));
        }
    }
}
