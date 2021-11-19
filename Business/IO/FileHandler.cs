using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business.IO
{
    public class FileHandler : IFileHandler
    {
        private readonly ICompression _compression;
        private readonly ILogging _logging;
        private readonly IHttpClientWrapper _httpClientWrapper;

        public FileHandler(
            ICompression compression,
            ILogging logging,
            IHttpClientWrapper httpClientWrapper
        )
        {
            _compression = compression;
            _logging = logging;
            _httpClientWrapper = httpClientWrapper;
        }

        public DateTime GetModifiedDate(string path)
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LastWriteTime;
        }

        public string GetFilePath(string path, string fileName)
        {
            return Path.Combine(path, fileName);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public Stream ReadStream(string path)
        {
            var reader = new StreamReader(new FileStream(path, FileMode.Open), Encoding.UTF8);
            return reader.BaseStream;
        }

        public string ReadTextFile(string path){
            if(string.IsNullOrEmpty(path) || !CheckIfFileExist(path)){
                return null;
            }

            using(var sr = new StreamReader(path)){
                return sr.ReadToEnd();
            }
        }

        public void WriteStream(string path, Stream stream)
        {
            using (var output = new FileStream(path, FileMode.Create))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(output);
            }
        }

        public string WriteXmlFile<T>(string path, string fileName, T content)
        {
            var outputPath = GetFilePath(path, fileName);
            var writer = new XmlSerializer(typeof(T));
            var file = File.Create(outputPath);

            _logging.Print($"Writing EPG-file to {outputPath}");

            writer.Serialize(file, content);
            file.Close();

            return outputPath;
        }

        public string WriteTextFile(string path, string fileName, string[] content)
        {
            var outputPath = GetFilePath(path, fileName);

            using (var writeFile = new StreamWriter(outputPath, false, new UTF8Encoding(true)))
            {
                foreach (string row in content)
                {
                    writeFile.WriteLine(row);
                }
            }

            return outputPath;
        }

        private Stream DownloadFile(string path)
        {
            return _httpClientWrapper.Get(path)?.Result;
        }

        public Stream GetSource(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _logging.Error($"  Source file path cannot be empty.");
                return null;
            }

            if (path.StartsWith("http"))
            {
                _logging.Print($"- Downloading file from {path}");
                var file = DownloadFile(path);

                if (file == null)
                {
                    _logging.Error($"  File is empty and/or corrupt.");
                    return null;
                }

                if (path.EndsWith(".gz"))
                {
                    _logging.Print($"- Decompressing file");
                    return _compression.Decompress(file);
                }

                return file;

            }
            else
            {
                _logging.Print($"- Reading local file from {path}");

                if (!File.Exists(path))
                {
                    return null;
                }

                var file = new FileStream(path, FileMode.Open);

                if (path.EndsWith(".gz"))
                {
                    _logging.Print($"- Decompressed file");
                    return _compression.Decompress(file);
                }

                return file;

            }
        }

        public bool CheckIfFileExist(string path){
            if(String.IsNullOrEmpty(path)){
                return false;
            }

            return File.Exists(path);
        }

        public bool CheckIfFolderExist(string path, bool create = false)
        {
            if (!Directory.Exists(path))
            {

                if (create)
                {
                    _logging.Info($"Couldn't find output folder, creating it at {path}!");
                    Directory.CreateDirectory(path);
                    return true;
                }

                return false;
            }

            return true;
        }
    }
}