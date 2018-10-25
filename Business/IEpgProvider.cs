using Couchpotato.Models;

public interface IEpgProvider{
    EpgList Load(string[] paths, Settings settings);
    void Save(string path, EpgList epgList);
}