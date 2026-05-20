using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class MediaScannerTests
    {
        [TestMethod]
        public void Scan_ClassifiesUnavailableFolderWithoutThrowing()
        {
            var scanner = new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance);

            var result = scanner.Scan(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

            Assert.AreEqual(ScanStatus.UnavailableOrError, result.Status);
            Assert.AreEqual(0, result.Files.Count);
        }

        [TestMethod]
        public void Scan_ReturnsSupportedFilesInDeterministicFilenameOrder()
        {
            var root = TestDirectory.Create();
            File.WriteAllText(Path.Combine(root.Path, "z.txt"), "ignore");
            File.WriteAllText(Path.Combine(root.Path, "b.png"), "image");
            File.WriteAllText(Path.Combine(root.Path, "a.JPG"), "image");
            Directory.CreateDirectory(Path.Combine(root.Path, "nested"));
            File.WriteAllText(Path.Combine(root.Path, "nested", "c.jpg"), "ignore recursive");

            var scanner = new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance);

            var result = scanner.Scan(root.Path);

            Assert.AreEqual(ScanStatus.Success, result.Status);
            CollectionAssert.AreEqual(new[] { "a.JPG", "b.png" }, result.Files.Select(f => f.Name).ToArray());
        }

        [TestMethod]
        public void Scan_ClassifiesAccessibleEmptyFolder()
        {
            var root = TestDirectory.Create();
            var scanner = new MediaScanner(new PhysicalFileSystem(), NullLogger.Instance);

            var result = scanner.Scan(root.Path);

            Assert.AreEqual(ScanStatus.SuccessEmpty, result.Status);
            Assert.AreEqual(0, result.Files.Count);
        }
    }
}
