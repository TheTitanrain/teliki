using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class ConfigFileStoreTests
    {
        [TestMethod]
        public void Save_PreservesUntouchedKeysCommentsAndRelativePath()
        {
            var root = TestDirectory.Create();
            var path = System.IO.Path.Combine(root.Path, "appsettings.ini");
            File.WriteAllText(
                path,
                "; comment\r\nMediaFolder=media\r\nIntervalSeconds=10\r\nCacheFolder=%LocalAppData%\\Teliki\\MediaCache\r\n");
            var store = new ConfigFileStore(new PhysicalFileSystem());

            var document = store.Load(path);
            document.SetEditableSettings(new EditableSettings("relative\\ads", 15, 7, 31, DisplayModeParser.SingleScreen, 2));
            store.Save(path, document);

            var saved = File.ReadAllText(path);
            StringAssert.Contains(saved, "; comment");
            StringAssert.Contains(saved, "MediaFolder=relative\\ads");
            StringAssert.Contains(saved, "IntervalSeconds=15");
            StringAssert.Contains(saved, "ScanIntervalSeconds=7");
            StringAssert.Contains(saved, "ScanTimeoutSeconds=31");
            StringAssert.Contains(saved, "ScreenMode=SingleScreen");
            StringAssert.Contains(saved, "ScreenIndex=2");
            StringAssert.Contains(saved, "CacheFolder=%LocalAppData%\\Teliki\\MediaCache");
        }

        [TestMethod]
        public void Save_DeduplicatesManagedDisplayKeysAtEffectivePosition()
        {
            var root = TestDirectory.Create();
            var path = Path.Combine(root.Path, "appsettings.ini");
            File.WriteAllText(
                path,
                "ScreenMode=AllScreens\r\nScreenIndex=1\r\nMediaFolder=media\r\nScreenMode=PrimaryScreen\r\nScreenIndex=3\r\n");
            var store = new ConfigFileStore(new PhysicalFileSystem());

            var document = store.Load(path);
            document.SetEditableSettings(new EditableSettings("media", 10, 5, 30, DisplayModeParser.SingleScreen, 2));
            store.Save(path, document);

            var saved = File.ReadAllText(path);
            Assert.AreEqual(1, CountOccurrences(saved, "ScreenMode="));
            Assert.AreEqual(1, CountOccurrences(saved, "ScreenIndex="));
            StringAssert.Contains(saved, "ScreenMode=SingleScreen");
            StringAssert.Contains(saved, "ScreenIndex=2");
        }

        [TestMethod]
        public void Save_MutedFalseSurvivesRoundTrip()
        {
            var root = TestDirectory.Create();
            var path = System.IO.Path.Combine(root.Path, "appsettings.ini");
            System.IO.File.WriteAllText(path, "Muted=false\r\n");
            var store = new ConfigFileStore(new PhysicalFileSystem());

            var document = store.Load(path);
            document.SetEditableSettings(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 0, false));
            store.Save(path, document);

            var saved = System.IO.File.ReadAllText(path);
            StringAssert.Contains(saved, "Muted=false");
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
