using System;
using System.IO;
using System.IO.Compression;

public class Compression : ICompression
{
    public void Compress(string path)
    {
         FileInfo sourceFile = new FileInfo(path);
            FileInfo targetFileName = new FileInfo($"{sourceFile.FullName}.gz");
                        
            using (FileStream sourceFileStream = sourceFile.OpenRead())
            {
                using (FileStream targetFileStream = targetFileName.Create())
                    {
                    using (GZipStream gzipStream = new GZipStream(targetFileStream, CompressionMode.Compress))
                    {
                        try
                        {
                            sourceFileStream.CopyTo(gzipStream);
                            Console.WriteLine($"Saving compressed file to {targetFileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Compression failed - {ex.Message}");
                        }
                    }
                }
            }
    }
}