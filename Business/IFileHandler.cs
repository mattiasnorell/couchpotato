
using System.IO;

public interface IFileHandler
{
    Stream GetSource(string path);
}