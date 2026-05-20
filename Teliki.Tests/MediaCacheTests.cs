using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class MediaCacheTests
    {
        [TestMethod]
        public void Promote_PreservesLastKnownGoodWhenScanUnavailable()
        {
            var source = TestDirectory.Create();
            var cache = TestDirectory.Create();
            File.WriteAllText(Path.Combine(source.Path, "a.jpg"), "one");

            var mediaCache = new MediaCache(new PhysicalFileSystem(), NullLogger.Instance);
            var first = mediaCache.Promote(new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance).Scan(source.Path), cache.Path);

            var second = mediaCache.Promote(ScanResult.Unavailable("network lost"), cache.Path);

            Assert.AreEqual(1, first.Items.Count);
            Assert.AreEqual(1, second.Items.Count);
            Assert.AreEqual(first.Items[0].CachedPath, second.Items[0].CachedPath);
            Assert.IsTrue(File.Exists(second.Items[0].CachedPath));
        }

        [TestMethod]
        public void Promote_RequiresTwoSuccessfulEmptyScansBeforeClearingPlaylist()
        {
            var source = TestDirectory.Create();
            var cache = TestDirectory.Create();
            File.WriteAllText(Path.Combine(source.Path, "a.jpg"), "one");

            var mediaCache = new MediaCache(new PhysicalFileSystem(), NullLogger.Instance);
            mediaCache.Promote(new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance).Scan(source.Path), cache.Path);

            var firstEmpty = mediaCache.Promote(ScanResult.SuccessEmpty(), cache.Path);
            var secondEmpty = mediaCache.Promote(ScanResult.SuccessEmpty(), cache.Path);

            Assert.AreEqual(1, firstEmpty.Items.Count);
            Assert.AreEqual(0, secondEmpty.Items.Count);
        }

        [TestMethod]
        public void Promote_CopiesToVersionedCachePathAndWritesManifest()
        {
            var source = TestDirectory.Create();
            var cache = TestDirectory.Create();
            File.WriteAllText(Path.Combine(source.Path, "ad.jpg"), "image");

            var mediaCache = new MediaCache(new PhysicalFileSystem(), NullLogger.Instance);
            var manifest = mediaCache.Promote(new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance).Scan(source.Path), cache.Path);

            Assert.AreEqual(1, manifest.Items.Count);
            Assert.IsTrue(File.Exists(manifest.Items[0].CachedPath));
            Assert.IsTrue(Path.GetFileName(manifest.Items[0].CachedPath).Contains("_"));
            Assert.IsTrue(File.Exists(Path.Combine(cache.Path, "manifest.ini")));
        }

        [TestMethod]
        public void Promote_SkipsFileThatDisappearsDuringCopyAndKeepsPreviousManifest()
        {
            var source = TestDirectory.Create();
            var cache = TestDirectory.Create();
            File.WriteAllText(Path.Combine(source.Path, "good.jpg"), "good");

            var mediaCache = new MediaCache(new PhysicalFileSystem(), NullLogger.Instance);
            var first = mediaCache.Promote(new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance).Scan(source.Path), cache.Path);

            var missing = new SourceMediaFile(Path.Combine(source.Path, "missing.jpg"), "missing.jpg", 10, DateTime.UtcNow);
            var failed = mediaCache.Promote(ScanResult.Success(new[] { missing }), cache.Path);

            Assert.AreEqual(first.Items.Single().CachedPath, failed.Items.Single().CachedPath);
        }

        [TestMethod]
        public void Promote_KeepsActiveMediaEvenWhenItExceedsCacheLimit()
        {
            var source = TestDirectory.Create();
            var cache = TestDirectory.Create();
            File.WriteAllText(Path.Combine(source.Path, "large.jpg"), "active media");

            var mediaCache = new MediaCache(new PhysicalFileSystem(), NullLogger.Instance);
            var manifest = mediaCache.Promote(
                new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance).Scan(source.Path),
                cache.Path,
                new CacheSettings(1, 0));

            Assert.AreEqual(1, manifest.Items.Count);
            Assert.IsTrue(File.Exists(manifest.Items[0].CachedPath));
        }
    }
}
