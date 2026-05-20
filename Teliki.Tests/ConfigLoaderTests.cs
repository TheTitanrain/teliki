using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class ConfigLoaderTests
    {
        [TestMethod]
        public void LoadFromText_UsesDefaultsAndExpandsEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("TELIKI_TEST_CACHE", "CacheRoot");

            var config = ConfigLoader.LoadFromText("CacheFolder=%TELIKI_TEST_CACHE%\\Teliki");

            Assert.AreEqual("media", config.MediaFolder);
            Assert.AreEqual(TimeSpan.FromSeconds(10), config.Interval);
            Assert.AreEqual(TimeSpan.FromSeconds(5), config.ScanInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(30), config.ScanTimeout);
            Assert.IsTrue(config.CacheFolder.EndsWith("CacheRoot\\Teliki", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(1024, config.MaxCacheSizeMb);
            Assert.AreEqual(512, config.MinFreeDiskMb);
            Assert.AreEqual("AllScreens", config.ScreenMode);
        }

        [TestMethod]
        public void LoadFromText_ClampsInvalidIntervalsAndCacheLimits()
        {
            var config = ConfigLoader.LoadFromText(
                "IntervalSeconds=0\r\nScanIntervalSeconds=-1\r\nScanTimeoutSeconds=0\r\nMaxCacheSizeMb=0\r\nMinFreeDiskMb=-2");

            Assert.AreEqual(TimeSpan.FromSeconds(1), config.Interval);
            Assert.AreEqual(TimeSpan.FromSeconds(1), config.ScanInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(1), config.ScanTimeout);
            Assert.AreEqual(1, config.MaxCacheSizeMb);
            Assert.AreEqual(0, config.MinFreeDiskMb);
        }
    }
}
