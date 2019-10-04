namespace Couchpotato.Business.Settings.Models
{
    public class UserSettingsCache{

        public UserSettingsCache() {
            Enabled = false;
            Lifespan = 25;
        }

        public bool Enabled{get;set;}
        public int Lifespan{get;set;}
    }
}
