using System.IO;

namespace Couchpotato.Business.Compression{
    public interface ICompression{
        void Compress(string path);
        Stream Decompress (Stream originalFileStream);
    }
}