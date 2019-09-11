
using System.IO;

namespace Couchpotato.Business.IO{
    public interface IFileHandler
    {
        Stream GetSource(string path);
        string WriteTextFile(string path, string fileName, string[] content);
        string WriteXmlFile<T>(string path, string fileName, T content);
        bool CheckIfFolderExist(string path, bool create = false);
        string GetFilePath(string path, string fileName);
    }
}