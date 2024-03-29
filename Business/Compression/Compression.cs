using System;
using System.IO;
using System.IO.Compression;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business.Compression
{
    public class Compression : ICompression
    {
        private readonly ILogging _logging;

        public Compression(
            ILogging logging
        )
        {
            _logging = logging;
        }

        public void Compress(string path)
        {
            var sourceFile = new FileInfo(path);
            var targetFileName = new FileInfo($"{sourceFile.FullName}.gz");

            using var sourceFileStream = File.Open(sourceFile.FullName, FileMode.Open, FileAccess.Read);
            using var targetFileStream = targetFileName.Create();
            using var gzipStream = new GZipStream(targetFileStream, CompressionMode.Compress);
            try
            {
                sourceFileStream.CopyTo(gzipStream);
                _logging.Print($"Saving compressed file to {targetFileName}");
            }
            catch (Exception ex)
            {
                _logging.Error($"Compression failed", ex);
            }
        }

        public Stream Decompress(Stream originalFileStream)
        {
            try
            {
                var decompressed = new MemoryStream();
                using (var zip = new GZipStream(originalFileStream, CompressionMode.Decompress, true))
                {
                    zip.CopyTo(decompressed);
                }

                decompressed.Seek(0, SeekOrigin.Begin);
                return decompressed;
            }
            catch (Exception ex)
            {
                _logging.Error($"- Decompression failed", ex);

                return null;
            }
        }
    }
}