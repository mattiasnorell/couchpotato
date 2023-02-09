using Couchpotato.Business.Cache;
using Couchpotato.Business.Compression;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Moq;
using NUnit.Framework;

namespace couchpotato.Tests
{
    [TestFixture]
    public class CacheProviderTests
    {

        private Mock<ICompression> _compressionMock;
        private Mock<ILogging> _loggingMock;
        private Mock<IHttpClientWrapper> _httpClientWrapperMock;
        private FileHandler _fileHandler;
        private CacheProvider _cacheProvider;

        [SetUp]
        public void SetUp()
        {
            _compressionMock = new Mock<ICompression>();
            _loggingMock = new Mock<ILogging>();
            _httpClientWrapperMock = new Mock<IHttpClientWrapper>();
            _fileHandler = new FileHandler(
                _compressionMock.Object,
                _loggingMock.Object,
                _httpClientWrapperMock.Object
            );
            _cacheProvider = new CacheProvider(_fileHandler);
        }
        
        [Test]
        public void CreateMD5_InputIsEmptyString_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = string.Empty;
            var expectedHash = "D4-1D-8C-D9-8F-00-B2-04-E9-80-09-98-EC-F8-42-7E";

            // Act
            var actualHash = _cacheProvider.CreateMD5(input);

            // Assert
            Assert.AreEqual(expectedHash, actualHash);
        }

        [Test]
        public void CreateMD5_InputIsHelloWorld_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "Hello World";
            var expectedHash = "B1-0A-8D-B1-64-E0-75-41-05-B7-A9-9B-E7-2E-3F-E5";

            // Act
            var actualHash = _cacheProvider.CreateMD5(input);

            // Assert
            Assert.AreEqual(expectedHash, actualHash);
        }

        [Test]
        public void CreateMD5_InputIsDifferentString_ShouldReturnDifferentHash()
        {
            // Arrange
            var input1 = "Hello World";
            var input2 = "Hello World!";

            // Act
            var hash1 = _cacheProvider.CreateMD5(input1);
            var hash2 = _cacheProvider.CreateMD5(input2);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }
    }
}