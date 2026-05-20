using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class SettingsValidatorTests
    {
        [TestMethod]
        public void Validate_RejectsEmptyFolderAndNonPositiveIntervals()
        {
            var errors = SettingsValidator.Validate(new EditableSettings("", 0, -1, 0, DisplayModeParser.AllScreens, 0));

            Assert.AreEqual(4, errors.Count);
        }

        [TestMethod]
        public void Validate_RejectsUnsupportedScreenMode()
        {
            var errors = SettingsValidator.Validate(new EditableSettings("media", 10, 5, 30, "LegacyMode", 1));

            CollectionAssert.Contains((System.Collections.ICollection)errors, "ScreenMode must be AllScreens, PrimaryScreen, or SingleScreen.");
        }

        [TestMethod]
        public void Validate_RejectsMissingScreenIndexForSingleScreenMode()
        {
            var errors = SettingsValidator.Validate(new EditableSettings("media", 10, 5, 30, DisplayModeParser.SingleScreen, 0));

            CollectionAssert.Contains((System.Collections.ICollection)errors, "ScreenIndex must be at least 1 when ScreenMode is SingleScreen.");
        }
    }
}
