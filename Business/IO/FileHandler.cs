using System;
using System.IO;
using System.Net;
using System.Text;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business.IO
{
    public class FileHandler:IFileHandler
    {
        private readonly ICompression _compression;
        private readonly ILogging _logging;

        public FileHandler(
            ICompression compression, 
            ILogging logging
        ){
            _compression = compression;
            _logging = logging;
        }

        public void WriteFile(string path, string[] content){
            using (System.IO.StreamWriter writeFile = new System.IO.StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writeFile.WriteLine("#EXTM3U");

                foreach (string row in content)
                {
                    writeFile.WriteLine(row);
                }
            }
        }

        private Stream DownloadFile(string path){
            using (var client = new WebClient())
            {
                try{
                    var result = client.OpenRead(path);

                    return result;
                }catch (Exception)
                {
                    return null;
                }
            }
        }

        public Stream GetSource(string path){
            if(path.StartsWith("http")){
                _logging.Print($"- Downloading file from {path}");
                var file = DownloadFile(path);

                if(path.EndsWith(".gz")){
                    _logging.Print($"- Decompressing file");

                    if(file == null){
                        _logging.Error($"  File is empty and/or corrupt. Can't decompress {path}");
                        return null;
                    }

                    return _compression.Decompress(file);
                }

                return file;
                
            }else{
                _logging.Print($"- Reading local file from {path}");

                if(!File.Exists(path)){
                    return null;
                }

                var file = new FileStream(path, FileMode.Open);

                if(path.EndsWith(".gz")){
                    _logging.Print($"- Decompressed file");
                    return _compression.Decompress(file);
                }

                return file;
            
            }
        }
    }
}