using System;
using System.IO;

namespace Couchpotato.Business.IO
{
    public interface IFileHandler
    {
        Stream GetSource(string path);
        string WriteTextFile(string path, string fileName, string[] content);
        string WriteXmlFile<T>(string path, string fileName, T content);
        void WriteStream(string path, Stream stream);
        Stream ReadStream(string path);
        string ReadTextFile(string path);
        bool CheckIfFolderExist(string path, bool create = false);
        string GetFilePath(string path, string fileName);
        bool CheckIfFileExist(string path);
        DateTime GetModifiedDate(string path);
    }
}