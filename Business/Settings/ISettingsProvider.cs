using Couchpotato.Business.Settings.Models;

namespace Couchpotato.Business.Settings
{
    public interface ISettingsProvider
    {
        UserSettings Load(string path);
    }
}