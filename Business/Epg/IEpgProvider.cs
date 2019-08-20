using Couchpotato.Business.Settings.Models;
using CouchpotatoShared.Epg;

namespace Couchpotato.Business{
    public interface IEpgProvider{
        EpgList GetProgramGuide(string[] paths, UserSettings settings);
        void Save(string path, EpgList epgList);
    }
}