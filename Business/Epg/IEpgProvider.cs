using Couchpotato.Core.Epg;

namespace Couchpotato.Business{
    public interface IEpgProvider{
        EpgList GetProgramGuide(string[] paths);
        string Save(string path, string fileName, EpgList epgList);
    }
}