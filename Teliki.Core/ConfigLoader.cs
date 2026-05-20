using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Teliki.Core
{
    public static class ConfigLoader
    {
        public static AppConfig Load(string path)
        {
            return File.Exists(path) ? LoadFromText(File.ReadAllText(path)) : LoadFromText(string.Empty);
        }

        public static AppConfig LoadFromText(string text)
        {
            var values = Parse(text);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var defaultCache = Path.Combine(localAppData, "Teliki", "MediaCache");

            return new AppConfig(
                Expand(Get(values, "MediaFolder", "media")),
                Seconds(GetInt(values, "IntervalSeconds", 10), 1),
                Seconds(GetInt(values, "ScanIntervalSeconds", 5), 1),
                Seconds(GetInt(values, "ScanTimeoutSeconds", 30), 1),
                Expand(Get(values, "CacheFolder", defaultCache)),
                Math.Max(1, GetInt(values, "MaxCacheSizeMb", 1024)),
                Math.Max(0, GetInt(values, "MinFreeDiskMb", 512)),
                Get(values, "ScreenMode", DisplayModeParser.AllScreens),
                GetInt(values, "ScreenIndex", 0));
        }

        private static Dictionary<string, string> Parse(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            var lines = text.Replace("\r\n", "\n").Split('\n');
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                {
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                result[key] = value;
            }

            return result;
        }

        private static string Get(Dictionary<string, string> values, string key, string defaultValue)
        {
            string value;
            return values.TryGetValue(key, out value) && value.Length > 0 ? value : defaultValue;
        }

        private static int GetInt(Dictionary<string, string> values, string key, int defaultValue)
        {
            string value;
            int parsed;
            return values.TryGetValue(key, out value) &&
                   int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)
                ? parsed
                : defaultValue;
        }

        private static TimeSpan Seconds(int value, int minimum)
        {
            return TimeSpan.FromSeconds(Math.Max(minimum, value));
        }

        private static string Expand(string value)
        {
            return Environment.ExpandEnvironmentVariables(value);
        }
    }
}
