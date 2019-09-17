using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Epg;

namespace Couchpotato.Business{
    public interface IEpgProvider{
        EpgList GetProgramGuide(string[] paths, UserSettings settings);
        string Save(string path, string fileName, EpgList epgList);
    }
}