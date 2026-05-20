using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Teliki.Core
{
    public sealed class ConfigDocument
    {
        private readonly List<ConfigLine> _lines;

        private ConfigDocument(List<ConfigLine> lines)
        {
            _lines = lines;
        }

        public static ConfigDocument Parse(string text)
        {
            var lines = new List<ConfigLine>();
            var split = (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            foreach (var raw in split)
            {
                string key;
                string value;
                var kind = ParseLine(raw, out key, out value);
                lines.Add(new ConfigLine(kind, raw, key, value));
            }

            return new ConfigDocument(lines);
        }

        public EditableSettings GetEditableSettings()
        {
            return new EditableSettings(
                GetValue("MediaFolder", "media"),
                GetInt("IntervalSeconds", 10),
                GetInt("ScanIntervalSeconds", 5),
                GetInt("ScanTimeoutSeconds", 30),
                GetValue("ScreenMode", DisplayModeParser.AllScreens),
                GetInt("ScreenIndex", 0));
        }

        public void SetEditableSettings(EditableSettings settings)
        {
            SetValue("MediaFolder", settings.MediaFolder);
            SetValue("IntervalSeconds", settings.IntervalSeconds.ToString(CultureInfo.InvariantCulture));
            SetValue("ScanIntervalSeconds", settings.ScanIntervalSeconds.ToString(CultureInfo.InvariantCulture));
            SetValue("ScanTimeoutSeconds", settings.ScanTimeoutSeconds.ToString(CultureInfo.InvariantCulture));
            SetValue("ScreenMode", settings.ScreenMode);
            SetValue("ScreenIndex", settings.ScreenIndex.ToString(CultureInfo.InvariantCulture));
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, _lines.Select(l => l.Raw));
        }

        private string GetValue(string key, string defaultValue)
        {
            for (var index = _lines.Count - 1; index >= 0; index--)
            {
                if (_lines[index].Kind == ConfigLineKind.KeyValue &&
                    string.Equals(_lines[index].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return _lines[index].Value;
                }
            }

            return defaultValue;
        }

        private int GetInt(string key, int defaultValue)
        {
            int value;
            return int.TryParse(GetValue(key, defaultValue.ToString(CultureInfo.InvariantCulture)), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : defaultValue;
        }

        private void SetValue(string key, string value)
        {
            var lastMatch = -1;
            for (var index = 0; index < _lines.Count; index++)
            {
                if (_lines[index].Kind == ConfigLineKind.KeyValue &&
                    string.Equals(_lines[index].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    lastMatch = index;
                }
            }

            if (lastMatch >= 0)
            {
                _lines[lastMatch] = ConfigLine.KeyValue(key, value);
                for (var index = _lines.Count - 1; index >= 0; index--)
                {
                    if (index == lastMatch)
                    {
                        continue;
                    }

                    if (_lines[index].Kind == ConfigLineKind.KeyValue &&
                        string.Equals(_lines[index].Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        _lines.RemoveAt(index);
                        if (index < lastMatch)
                        {
                            lastMatch--;
                        }
                    }
                }

                return;
            }

            if (_lines.Count > 0 && _lines[_lines.Count - 1].Raw.Length != 0)
            {
                _lines.Add(new ConfigLine(ConfigLineKind.Blank, string.Empty, null, null));
            }

            _lines.Add(ConfigLine.KeyValue(key, value));
        }

        private static ConfigLineKind ParseLine(string raw, out string key, out string value)
        {
            key = null;
            value = null;
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return ConfigLineKind.Blank;
            }

            if (trimmed.StartsWith(";") || trimmed.StartsWith("#"))
            {
                return ConfigLineKind.Comment;
            }

            var separator = raw.IndexOf('=');
            if (separator <= 0)
            {
                return ConfigLineKind.Other;
            }

            key = raw.Substring(0, separator).Trim();
            value = raw.Substring(separator + 1).Trim();
            return ConfigLineKind.KeyValue;
        }

        private enum ConfigLineKind
        {
            Blank,
            Comment,
            KeyValue,
            Other
        }

        private struct ConfigLine
        {
            public ConfigLine(ConfigLineKind kind, string raw, string key, string value)
            {
                Kind = kind;
                Raw = raw;
                Key = key;
                Value = value;
            }

            public ConfigLineKind Kind { get; private set; }
            public string Raw { get; private set; }
            public string Key { get; private set; }
            public string Value { get; private set; }

            public static ConfigLine KeyValue(string key, string value)
            {
                return new ConfigLine(ConfigLineKind.KeyValue, key + "=" + value, key, value);
            }
        }
    }
}
