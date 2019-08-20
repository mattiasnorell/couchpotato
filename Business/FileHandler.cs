using System;
using System.IO;
using System.Net;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business
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
                    return client.OpenRead(path);
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
                    return compression.Decompress(file);
                }

                return file;
                
            }else{
                this.logging.Print($"- Reading local file from {path}");

                if(!File.Exists(path)){
                    return null;
                }

                var file =  new FileStream(path, FileMode.Open);

                if(path.EndsWith(".gz")){
                    Console.WriteLine($"- Decompressed file");
                    return compression.Decompress(file);
                }

                return file;
            
            }
        }
    }
}