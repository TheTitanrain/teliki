using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class DisplayScreenSelectorTests
    {
        [TestMethod]
        public void SelectScreens_AllScreens_ReturnsEveryScreen()
        {
            var screens = CreateScreens();
            var result = DisplayScreenSelector.SelectScreens(CreateConfig(DisplayModeParser.AllScreens, 0), screens);

            Assert.AreEqual(2, result.Screens.Count);
            Assert.IsFalse(result.UsedFallback);
        }

        [TestMethod]
        public void SelectScreens_PrimaryScreen_ReturnsOnlyPrimary()
        {
            var screens = CreateScreens();
            var result = DisplayScreenSelector.SelectScreens(CreateConfig(DisplayModeParser.PrimaryScreen, 0), screens);

            Assert.AreEqual(1, result.Screens.Count);
            Assert.AreEqual(1, result.Screens[0].Index);
        }

        [TestMethod]
        public void SelectScreens_SingleScreen_ReturnsConfiguredScreen()
        {
            var screens = CreateScreens();
            var result = DisplayScreenSelector.SelectScreens(CreateConfig(DisplayModeParser.SingleScreen, 2), screens);

            Assert.AreEqual(1, result.Screens.Count);
            Assert.AreEqual(2, result.Screens[0].Index);
            Assert.IsFalse(result.UsedFallback);
        }

        [TestMethod]
        public void SelectScreens_MissingScreen_FallsBackToPrimary()
        {
            var screens = CreateScreens();
            var result = DisplayScreenSelector.SelectScreens(CreateConfig(DisplayModeParser.SingleScreen, 5), screens);

            Assert.AreEqual(1, result.Screens.Count);
            Assert.AreEqual(1, result.Screens[0].Index);
            Assert.IsTrue(result.UsedFallback);
            StringAssert.Contains(result.Warning, "Falling back");
        }

        private static AppConfig CreateConfig(string mode, int screenIndex)
        {
            return new AppConfig(
                "media",
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30),
                "cache",
                1024,
                512,
                mode,
                screenIndex);
        }

        private static DisplayScreen[] CreateScreens()
        {
            return new[]
            {
                new DisplayScreen(1, 0, 0, 1920, 1080, true),
                new DisplayScreen(2, 1920, 0, 1280, 1024, false)
            };
        }
    }
}
