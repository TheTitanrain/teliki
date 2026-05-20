using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class DisplayCoordinatorTests
    {
        [TestMethod]
        public void Advance_RendersNextPlaylistItemOnEveryRenderer()
        {
            var playlist = new PlaylistService();
            playlist.Replace(new PlaylistManifest(new[]
            {
                new CachedMediaItem("a.jpg", "a.jpg", MediaKind.Image),
                new CachedMediaItem("b.jpg", "b.jpg", MediaKind.Image)
            }));
            var first = new RecordingMediaRenderer();
            var second = new RecordingMediaRenderer();
            var coordinator = new DisplayCoordinator(playlist, new[] { first, second }, NullLogger.Instance);

            coordinator.Advance();
            coordinator.Advance();

            CollectionAssert.AreEqual(new[] { "a.jpg", "b.jpg" }, first.RenderedPaths.ToArray());
            CollectionAssert.AreEqual(new[] { "a.jpg", "b.jpg" }, second.RenderedPaths.ToArray());
        }

        [TestMethod]
        public void Advance_ShowsBlankWhenPlaylistIsEmpty()
        {
            var playlist = new PlaylistService();
            playlist.Replace(PlaylistManifest.Empty);
            var renderer = new RecordingMediaRenderer();
            var coordinator = new DisplayCoordinator(playlist, new[] { renderer }, NullLogger.Instance);

            coordinator.Advance();

            Assert.AreEqual(1, renderer.BlankCount);
        }

        private sealed class RecordingMediaRenderer : IMediaRenderer
        {
            public readonly List<string> RenderedPaths = new List<string>();
            public int BlankCount;

            public void Render(CachedMediaItem item)
            {
                RenderedPaths.Add(item.CachedPath);
            }

            public void ShowBlank()
            {
                BlankCount++;
            }
        }
    }
}
