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
            document.SetEditableSettings(new EditableSettings("relative\\ads", 15, 7, 31));
            store.Save(path, document);

            var saved = File.ReadAllText(path);
            StringAssert.Contains(saved, "; comment");
            StringAssert.Contains(saved, "MediaFolder=relative\\ads");
            StringAssert.Contains(saved, "IntervalSeconds=15");
            StringAssert.Contains(saved, "ScanIntervalSeconds=7");
            StringAssert.Contains(saved, "ScanTimeoutSeconds=31");
            StringAssert.Contains(saved, "CacheFolder=%LocalAppData%\\Teliki\\MediaCache");
        }
    }
}
