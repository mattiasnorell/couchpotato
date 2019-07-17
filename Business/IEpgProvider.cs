using Couchpotato.Models;
using CouchpotatoShared.Epg;

namespace Couchpotato.Business{
    public interface IEpgProvider{
        EpgList GetProgramGuide(string[] paths, Settings settings);
        void Save(string path, EpgList epgList);
    }
}