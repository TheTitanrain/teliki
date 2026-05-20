using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.App;

namespace Teliki.Tests
{
    [TestClass]
    public class CursorVisibilityManagerTests
    {
        [TestMethod]
        public void HideForPlayback_HidesCursorOnlyOnce_AndRestoresAroundModalUi()
        {
            var cursor = new RecordingCursorController();
            var manager = new CursorVisibilityManager(cursor);

            manager.HideForPlayback();
            manager.HideForPlayback();

            using (manager.ShowCursorWhileModalUiOpen())
            {
                Assert.AreEqual(1, cursor.HideCalls);
                Assert.AreEqual(1, cursor.ShowCalls);
            }

            Assert.AreEqual(2, cursor.HideCalls);
            Assert.AreEqual(1, cursor.ShowCalls);
        }

        private sealed class RecordingCursorController : ICursorController
        {
            public int HideCalls { get; private set; }
            public int ShowCalls { get; private set; }

            public void Hide()
            {
                HideCalls++;
            }

            public void Show()
            {
                ShowCalls++;
            }
        }
    }
}
