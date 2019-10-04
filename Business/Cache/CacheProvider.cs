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
        private string _cachePath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/cache";
        private readonly IFileHandler _fileHandler;

        public CacheProvider(IFileHandler fileHandler)
        {
            _fileHandler = fileHandler;
        }

        public Stream Get(string key, int timespan)
        {
            var keyHash = CreateMD5(key);
            var filePath = _fileHandler.GetFilePath(_cachePath, keyHash);

            if(_fileHandler.GetModifiedDate(filePath) < DateTime.Now.AddHours(-timespan)){
                return null;
            }

            return _fileHandler.ReadStream(filePath);
        }

        public void Set(string key, Stream stream)
        {

            _fileHandler.CheckIfFolderExist(_cachePath, true);

            var keyHash = CreateMD5(key);
            var filePath = _fileHandler.GetFilePath(_cachePath, keyHash);
            
            _fileHandler.WriteStream(filePath, stream);
        }

        public static string CreateMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash);
            }
        }
    }
}