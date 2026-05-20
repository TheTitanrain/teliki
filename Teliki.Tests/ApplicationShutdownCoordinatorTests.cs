using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.App;

namespace Teliki.Tests
{
    [TestClass]
    public class ApplicationShutdownCoordinatorTests
    {
        [TestMethod]
        public void ShouldExitThreadAfterFormClosed_WhenExitRequested_ReturnsTrueEvenIfFormsRemain()
        {
            var coordinator = new ApplicationShutdownCoordinator();
            coordinator.RequestExit();

            Assert.IsTrue(coordinator.ShouldExitThreadAfterFormClosed(2));
        }

        [TestMethod]
        public void ShouldExitThreadAfterFormClosed_WhenNoFormsRemain_ReturnsTrue()
        {
            var coordinator = new ApplicationShutdownCoordinator();

            Assert.IsTrue(coordinator.ShouldExitThreadAfterFormClosed(0));
        }

        [TestMethod]
        public void RequestExit_ReturnsFalseAfterFirstCall()
        {
            var coordinator = new ApplicationShutdownCoordinator();

            Assert.IsTrue(coordinator.RequestExit());
            Assert.IsFalse(coordinator.RequestExit());
        }
    }
}
