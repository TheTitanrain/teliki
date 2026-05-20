using System.Collections.Generic;

namespace Teliki.Core
{
    public sealed class EditableSettings
    {
        public EditableSettings(
            string mediaFolder,
            int intervalSeconds,
            int scanIntervalSeconds,
            int scanTimeoutSeconds,
            string screenMode,
            int screenIndex)
        {
            MediaFolder = mediaFolder;
            IntervalSeconds = intervalSeconds;
            ScanIntervalSeconds = scanIntervalSeconds;
            ScanTimeoutSeconds = scanTimeoutSeconds;
            ScreenMode = screenMode;
            ScreenIndex = screenIndex;
        }

        public string MediaFolder { get; private set; }
        public int IntervalSeconds { get; private set; }
        public int ScanIntervalSeconds { get; private set; }
        public int ScanTimeoutSeconds { get; private set; }
        public string ScreenMode { get; private set; }
        public int ScreenIndex { get; private set; }
    }

    public static class SettingsValidator
    {
        public static IReadOnlyList<string> Validate(EditableSettings settings)
        {
            var errors = new List<string>();
            if (settings == null)
            {
                errors.Add("Settings are required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(settings.MediaFolder))
            {
                errors.Add("Media folder is required.");
            }

            if (settings.IntervalSeconds < 1)
            {
                errors.Add("IntervalSeconds must be at least 1.");
            }

            if (settings.ScanIntervalSeconds < 1)
            {
                errors.Add("ScanIntervalSeconds must be at least 1.");
            }

            if (settings.ScanTimeoutSeconds < 1)
            {
                errors.Add("ScanTimeoutSeconds must be at least 1.");
            }

            var canonicalMode = DisplayModeParser.Canonicalize(settings.ScreenMode);
            if (!string.Equals(canonicalMode, settings.ScreenMode, System.StringComparison.Ordinal))
            {
                errors.Add("ScreenMode must be AllScreens, PrimaryScreen, or SingleScreen.");
            }

            if (string.Equals(settings.ScreenMode, DisplayModeParser.SingleScreen, System.StringComparison.Ordinal) &&
                settings.ScreenIndex < 1)
            {
                errors.Add("ScreenIndex must be at least 1 when ScreenMode is SingleScreen.");
            }

            return errors;
        }
    }
}
