namespace Couchpotato.Business.Settings.Models
{
    public class UserSettingsEpg{

        public UserSettingsEpg(){
            Cache = new UserSettingsCache();
        }

        public UserSettingsCache Cache{get;set;}
        public string[] Paths{get;set;}
    }
}
