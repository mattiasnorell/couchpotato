using System.IO;

public interface ICompression{
    void Compress(string path);
    Stream Decompress (Stream originalFileStream);
}