
using System.IO;

namespace Couchpotato.Business.IO{
    public interface IFileHandler
    {
        Stream GetSource(string path);
        void WriteFile(string path, string[] content);
    }
}