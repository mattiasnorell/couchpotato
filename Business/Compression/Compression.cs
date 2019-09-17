using System;
using System.IO;
using System.IO.Compression;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business.Compression{
    public class Compression : ICompression
    {
        private readonly ILogging _logging;

        public Compression(
            ILogging logging
        ){
            _logging = logging;
        }
        
        public void Compress(string path)
        {
            var sourceFile = new FileInfo(path);
            var targetFileName = new FileInfo($"{sourceFile.FullName}.gz");
                        
            using (var sourceFileStream = sourceFile.OpenRead())
            {
                using (var targetFileStream = targetFileName.Create())
                    {
                    using (var gzipStream = new GZipStream(targetFileStream, CompressionMode.Compress))
                    {
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
                }
            }
        }

        public Stream Decompress(Stream originalFileStream){
            try{
                return new GZipStream(originalFileStream, CompressionMode.Decompress);
            }catch(Exception ex){
                _logging.Error($"- Decompression failed", ex);
                
                return null;
            }
        }
    }
}