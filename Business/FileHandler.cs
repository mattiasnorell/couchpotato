using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Couchpotato.Business{
    public class FileHandler:IFileHandler
    {
        private readonly ICompression compression;

        public FileHandler(ICompression compression){
            this.compression = compression;
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
                Console.WriteLine($"- Downloading file from {path}");
                var file = DownloadFile(path);

                if(path.EndsWith(".gz")){
                    Console.WriteLine($"- Decompressed file");
                    return compression.Decompress(file);
                }

                return file;
                
            }else{
                Console.WriteLine($"- Reading local file from {path}");

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