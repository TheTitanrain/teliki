using Microsoft.Win32;

namespace Teliki.App
{
    internal static class AutostartManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Teliki";

        internal static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                return key?.GetValue(ValueName) != null;
        }

        internal static void Enable(string exePath)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                key?.SetValue(ValueName, "\"" + exePath + "\"");
        }

        internal static void Disable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                key?.DeleteValue(ValueName, false);
        }
    }
}
