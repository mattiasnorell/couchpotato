using System;
using System.IO;
using System.Net;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business.IO
{
    public class FileHandler:IFileHandler
    {
        private readonly ICompression compression;
        private readonly ILogging logging;

        public FileHandler(
            ICompression compression, 
            ILogging logging
        ){
            this.compression = compression;
            this.logging = logging;
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
                this.logging.Print($"- Downloading file from {path}");
                var file = DownloadFile(path);

                if(path.EndsWith(".gz")){
                    this.logging.Print($"- Decompressing file");

                    if(file == null){
                        this.logging.Error($"  File is empty and/or corrupt. Can't decompress {path}");
                        return null;
                    }

                    return compression.Decompress(file);
                }

                return file;
                
            }else{
                this.logging.Print($"- Reading local file from {path}");

                if(!File.Exists(path)){
                    return null;
                }

                var file = new FileStream(path, FileMode.Open);

                if(path.EndsWith(".gz")){
                    this.logging.Print($"- Decompressed file");
                    return compression.Decompress(file);
                }

                return file;
            
            }
        }
    }
}