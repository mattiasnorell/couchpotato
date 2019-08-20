using Couchpotato.Business.Settings.Models;

namespace Couchpotato.Business{
    public interface ISettingsProvider{
        UserSettings Load(string path);
    }
}