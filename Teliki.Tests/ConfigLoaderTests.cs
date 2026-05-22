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
            Assert.AreEqual(DisplayModeParser.AllScreens, config.ScreenMode);
            Assert.AreEqual(DisplayTargetMode.AllScreens, config.DisplayMode);
            Assert.AreEqual(0, config.ScreenIndex);
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

        [TestMethod]
        public void LoadFromText_ParsesDisplayModeAndScreenIndex()
        {
            var config = ConfigLoader.LoadFromText("ScreenMode=SingleScreen\r\nScreenIndex=2");

            Assert.AreEqual(DisplayModeParser.SingleScreen, config.ScreenMode);
            Assert.AreEqual(DisplayTargetMode.SingleScreen, config.DisplayMode);
            Assert.AreEqual(2, config.ScreenIndex);
        }

        [TestMethod]
        public void LoadFromText_UnknownDisplayModeFallsBackToAllScreens()
        {
            var config = ConfigLoader.LoadFromText("ScreenMode=LegacyMode\r\nScreenIndex=3");

            Assert.AreEqual("LegacyMode", config.ScreenMode);
            Assert.AreEqual(DisplayTargetMode.AllScreens, config.DisplayMode);
            Assert.AreEqual(3, config.ScreenIndex);
        }

        [TestMethod]
        public void LoadFromText_MutedDefaultsToTrue()
        {
            var config = ConfigLoader.LoadFromText(string.Empty);
            Assert.IsTrue(config.Muted);
        }

        [TestMethod]
        public void LoadFromText_MutedFalseIsRespected()
        {
            var config = ConfigLoader.LoadFromText("Muted=false");
            Assert.IsFalse(config.Muted);
        }

        [TestMethod]
        public void LoadFromText_MutedTrueIsRespected()
        {
            var config = ConfigLoader.LoadFromText("Muted=true");
            Assert.IsTrue(config.Muted);
        }

        [TestMethod]
        public void Normalize_PreservesMutedFalse()
        {
            var config = ConfigLoader.LoadFromText("Muted=false");
            var normalized = AppConfigNormalizer.Normalize(config, "C:\\base");
            Assert.IsFalse(normalized.Muted);
        }
    }
}
