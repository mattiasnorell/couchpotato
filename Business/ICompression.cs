using System.IO;

namespace Couchpotato.Business{
    public interface ICompression{
        void Compress(string path);
        Stream Decompress (Stream originalFileStream);
    }
}