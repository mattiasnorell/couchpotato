using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Reflection;
using Couchpotato.Business.IO;

namespace Couchpotato.Business.Cache
{
    public class CacheProvider : ICacheProvider
    {
        private readonly string _cachePath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)}\\cache";
        private readonly IFileHandler _fileHandler;

        public CacheProvider(IFileHandler fileHandler)
        {
            _fileHandler = fileHandler;
        }

        public Stream Get(string key, int timespan)
        {
            var keyHash = CreateMD5(key);
            var filePath = _fileHandler.GetFilePath(_cachePath, keyHash);

            if (!_fileHandler.FileExists(filePath))
            {
                return null;
            }

            return _fileHandler.GetModifiedDate(filePath) < DateTime.Now.AddHours(-timespan)
                ? null
                : _fileHandler.ReadStream(filePath);
        }

        public void Set(string key, Stream stream)
        {
            _fileHandler.CheckIfFolderExist(_cachePath, true);

            var keyHash = CreateMD5(key);
            var filePath = _fileHandler.GetFilePath(_cachePath, keyHash);

            _fileHandler.WriteStream(filePath, stream);
        }

        public string CreateMD5(string input)
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash);
        }
    }
}