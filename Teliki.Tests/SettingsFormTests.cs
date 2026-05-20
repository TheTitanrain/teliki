using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.App;
using Teliki.Core;
using System.Windows.Forms;

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
                Assert.IsTrue(form.HasScreenHelperLabel);
                Assert.AreEqual("Target screen is used only when Display mode is set to Single monitor.", form.ScreenHelperText);
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
        public void Buttons_AreAssignedToAcceptAndCancelActions()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1)))
            {
                Assert.AreSame(FindControl<Button>(form, "SaveButton"), form.AcceptButton);
                Assert.AreSame(FindControl<Button>(form, "CancelButton"), form.CancelButton);
            }
        }

        [TestMethod]
        public void TextFieldChanges_MarkFormDirty()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1)))
            {
                Assert.IsFalse(form.IsDirty);

                FindControl<TextBox>(form, "MediaFolderTextBox").Text = "updated-media";

                Assert.IsTrue(form.IsDirty);
            }
        }

        [TestMethod]
        public void NumericFieldChanges_MarkFormDirty()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1)))
            {
                Assert.IsFalse(form.IsDirty);

                FindControl<NumericUpDown>(form, "ScanTimeoutNumeric").Value = 31;

                Assert.IsTrue(form.IsDirty);
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

        [TestMethod]
        public void Save_Success_ClearsDirty()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1)))
            {
                FindControl<TextBox>(form, "MediaFolderTextBox").Text = "updated-media";
                Assert.IsTrue(form.IsDirty);

                form.PerformSave();

                Assert.IsFalse(form.IsDirty);
            }
        }

        [TestMethod]
        public void HelperLabel_RemainsPresentWhenSwitchingDisplayModes()
        {
            using (var form = CreateForm(new EditableSettings("media", 10, 5, 30, DisplayModeParser.AllScreens, 1)))
            {
                form.SelectScreenMode(DisplayModeParser.SingleScreen);
                Assert.IsTrue(form.HasScreenHelperLabel);
                Assert.AreEqual("Target screen is used only when Display mode is set to Single monitor.", form.ScreenHelperText);

                form.SelectScreenMode(DisplayModeParser.PrimaryScreen);
                Assert.IsTrue(form.HasScreenHelperLabel);
                Assert.AreEqual("Target screen is used only when Display mode is set to Single monitor.", form.ScreenHelperText);
            }
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

        private static TControl FindControl<TControl>(Control root, string name) where TControl : Control
        {
            var match = TryFindControl<TControl>(root, name);
            if (match != null)
            {
                return match;
            }

            Assert.Fail("Control '{0}' of type '{1}' was not found.", name, typeof(TControl).Name);
            return null;
        }

        private static TControl TryFindControl<TControl>(Control root, string name) where TControl : Control
        {
            if (root is TControl && root.Name == name)
            {
                return (TControl)root;
            }

            foreach (Control child in root.Controls)
            {
                var match = TryFindControl<TControl>(child, name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
