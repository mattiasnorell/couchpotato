using System;
using System.IO;
using System.Net;
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

        public FileHandler(
            ICompression compression,
            ILogging logging
        )
        {
            _compression = compression;
            _logging = logging;
        }

        public string GetFilePath(string path, string fileName)
        {
            return Path.Combine(path, fileName);
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
            using (var client = new WebClient())
            {
                try
                {
                    var result = client.OpenRead(path);

                    return result;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public Stream GetSource(string path)
        {
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