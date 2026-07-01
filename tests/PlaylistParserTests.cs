using Couchpotato.Business.Logging;
using Couchpotato.Business.Playlist;
using Moq;
using NUnit.Framework;

namespace couchpotato.Tests
{
    [TestFixture]
    public class PlaylistParserTests
    {
        private Mock<ILogging> _loggingMock;
        private PlaylistParser _playlistParser;

        [SetUp]
        public void SetUp()
        {
            _loggingMock = new Mock<ILogging>();
            _playlistParser = new PlaylistParser(_loggingMock.Object);
        }

        [Test]
        public void Parse_WellFormedPlaylist_ShouldReturnMappedItems()
        {
            // Arrange
            var file = new[]
            {
                "#EXTM3U",
                "#EXTINF:-1 tvg-id=\"id1\" tvg-name=\"Channel 1\" tvg-logo=\"logo1\" group-title=\"News\",Channel 1",
                "http://example.com/1",
                "#EXTINF:-1 tvg-id=\"id2\" tvg-name=\"Channel 2\" tvg-logo=\"logo2\" group-title=\"Movies\",Channel 2",
                "http://example.com/2"
            };

            // Act
            var result = _playlistParser.Parse(file);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel 1"));
            Assert.AreEqual("http://example.com/1", result["Channel 1"].Url);
            Assert.AreEqual("News", result["Channel 1"].GroupTitle);
            Assert.AreEqual("http://example.com/2", result["Channel 2"].Url);
        }

        [Test]
        public void Parse_EvenLengthPlaylistWithTrailingLine_ShouldNotThrow()
        {
            // Arrange - trailing empty line makes the array even-length,
            // which previously caused an IndexOutOfRangeException.
            var file = new[]
            {
                "#EXTM3U",
                "#EXTINF:-1 tvg-id=\"id1\" tvg-name=\"Channel 1\" tvg-logo=\"logo1\" group-title=\"News\",Channel 1",
                "http://example.com/1",
                ""
            };

            // Act
            var result = _playlistParser.Parse(file);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel 1"));
            Assert.AreEqual("http://example.com/1", result["Channel 1"].Url);
        }

        [Test]
        public void Parse_EmptyArray_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var file = new string[] { };

            // Act
            var result = _playlistParser.Parse(file);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Parse_DuplicateTvgName_ShouldKeepFirstOccurrence()
        {
            // Arrange
            var file = new[]
            {
                "#EXTM3U",
                "#EXTINF:-1 tvg-id=\"id1\" tvg-name=\"Channel 1\" tvg-logo=\"logo1\" group-title=\"News\",Channel 1",
                "http://example.com/first",
                "#EXTINF:-1 tvg-id=\"id2\" tvg-name=\"Channel 1\" tvg-logo=\"logo2\" group-title=\"Movies\",Channel 1",
                "http://example.com/second"
            };

            // Act
            var result = _playlistParser.Parse(file);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("http://example.com/first", result["Channel 1"].Url);
        }
    }
}
