using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class PlaylistServiceTests
    {
        [TestMethod]
        public void Next_ReturnsNullForEmptyPlaylist()
        {
            var playlist = new PlaylistService();
            playlist.Replace(PlaylistManifest.Empty);

            Assert.IsNull(playlist.Next());
        }

        [TestMethod]
        public void Next_RepeatsSingleItem()
        {
            var playlist = new PlaylistService();
            playlist.Replace(new PlaylistManifest(new[] { new CachedMediaItem("a", "a.jpg", MediaKind.Image) }));

            Assert.AreEqual("a", playlist.Next().CachedPath);
            Assert.AreEqual("a", playlist.Next().CachedPath);
        }

        [TestMethod]
        public void Next_CyclesMultipleItems()
        {
            var playlist = new PlaylistService();
            playlist.Replace(new PlaylistManifest(new[]
            {
                new CachedMediaItem("a", "a.jpg", MediaKind.Image),
                new CachedMediaItem("b", "b.jpg", MediaKind.Image)
            }));

            Assert.AreEqual("a", playlist.Next().CachedPath);
            Assert.AreEqual("b", playlist.Next().CachedPath);
            Assert.AreEqual("a", playlist.Next().CachedPath);
        }

        [TestMethod]
        public void ReportFailure_QuarantinesItemAfterThreeFailures()
        {
            var playlist = new PlaylistService();
            var bad = new CachedMediaItem("bad.jpg", "bad.jpg", MediaKind.Image);
            playlist.Replace(new PlaylistManifest(new[]
            {
                bad,
                new CachedMediaItem("good.jpg", "good.jpg", MediaKind.Image)
            }));

            playlist.ReportFailure(bad);
            playlist.ReportFailure(bad);
            playlist.ReportFailure(bad);

            Assert.AreEqual("good.jpg", playlist.Next().CachedPath);
            Assert.AreEqual("good.jpg", playlist.Next().CachedPath);
        }
    }
}
