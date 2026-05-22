using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class MediaFilenameParserTests
    {
        [TestMethod]
        public void ParseDuration_WithValidSuffix_ReturnsDuration()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(30), MediaFilenameParser.ParseDuration("slide_30s.jpg"));
        }

        [TestMethod]
        public void ParseDuration_WithNoSuffix_ReturnsNull()
        {
            Assert.IsNull(MediaFilenameParser.ParseDuration("slide.jpg"));
        }

        [TestMethod]
        public void ParseDuration_WithZeroSeconds_ReturnsNull()
        {
            Assert.IsNull(MediaFilenameParser.ParseDuration("slide_0s.jpg"));
        }

        [TestMethod]
        public void ParseDuration_WithNullInput_ReturnsNull()
        {
            Assert.IsNull(MediaFilenameParser.ParseDuration(null));
        }

        [TestMethod]
        public void ParseDuration_IsCaseInsensitive()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(10), MediaFilenameParser.ParseDuration("slide_10S.jpg"));
        }

        [TestMethod]
        public void ParseDuration_WithSuffixInMiddle_ReturnsNull()
        {
            Assert.IsNull(MediaFilenameParser.ParseDuration("slide_5s_extra.jpg"));
        }

        [TestMethod]
        public void CachedMediaItem_WithDurationSuffix_HasDuration()
        {
            var item = new CachedMediaItem("p", "slide_30s.jpg", MediaKind.Image);
            Assert.AreEqual(TimeSpan.FromSeconds(30), item.Duration);
        }

        [TestMethod]
        public void CachedMediaItem_WithoutSuffix_HasNullDuration()
        {
            var item = new CachedMediaItem("p", "slide.jpg", MediaKind.Image);
            Assert.IsNull(item.Duration);
        }
    }
}
