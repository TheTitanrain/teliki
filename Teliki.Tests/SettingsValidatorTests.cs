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
            var errors = SettingsValidator.Validate(new EditableSettings("", 0, -1, 0));

            Assert.AreEqual(4, errors.Count);
        }
    }
}
