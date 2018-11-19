
using System.IO;

namespace Couchpotato.Business{
    public interface IFileHandler
    {
        Stream GetSource(string path);
    }
}