using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.App;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class SettingsFormTests
    {
        [TestMethod]
        public void Constructor_UsesProvidedDisplaySelection()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.SingleScreen, 2)))
            {
                Assert.AreEqual(DisplayModeParser.SingleScreen, form.SelectedScreenMode);
                Assert.AreEqual(2, form.SelectedScreenIndex);
                Assert.IsTrue(form.IsScreenSelectorEnabled);
            }
        }

        [TestMethod]
        public void SelectScreenMode_TogglesScreenSelectorAndPreservesSelection()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.SingleScreen, 2)))
            {
                form.SelectScreenMode(DisplayModeParser.AllScreens);
                Assert.IsFalse(form.IsScreenSelectorEnabled);
                Assert.AreEqual(2, form.SelectedScreenIndex);

                form.SelectScreenMode(DisplayModeParser.SingleScreen);
                Assert.IsTrue(form.IsScreenSelectorEnabled);
                Assert.AreEqual(2, form.SelectedScreenIndex);
            }
        }

        [TestMethod]
        public void SelectionChanges_MarkFormDirty()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1)))
            {
                Assert.IsFalse(form.IsDirty);

                form.SelectScreenMode(DisplayModeParser.SingleScreen);

                Assert.IsTrue(form.IsDirty);
            }
        }

        [TestMethod]
        public void Save_UsesCurrentDisplayModeAndScreenSelection()
        {
            EditableSettings saved = null;
            using (var form = CreateForm(
                       new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1),
                       delegate(EditableSettings settings)
                       {
                           saved = settings;
                           return true;
                       }))
            {
                form.SelectScreenMode(DisplayModeParser.SingleScreen);
                form.SelectScreenIndex(2);
                form.PerformSave();
            }

            Assert.IsNotNull(saved);
            Assert.AreEqual(DisplayModeParser.SingleScreen, saved.ScreenMode);
            Assert.AreEqual(2, saved.ScreenIndex);
        }

        private static SettingsForm CreateForm(EditableSettings settings, System.Func<EditableSettings, bool> saveHandler = null)
        {
            return new SettingsForm(
                settings,
                new List<DisplayScreen>
                {
                    new DisplayScreen(1, 0, 0, 1920, 1080, true),
                    new DisplayScreen(2, 1920, 0, 1280, 1024, false)
                },
                saveHandler ?? (delegate { return true; }),
                delegate { return true; });
        }
    }
}
