using System.Collections.Generic;

namespace Couchpotato.Business.Settings.Models
{
    public class UserSettingsValidation{

        public UserSettingsValidation(){
            Enabled = false;
            ContentTypes = new string[]{};
            ShowInvalid = false;
        }

        public bool Enabled{get;set;}
        public string[] ContentTypes { get; set; }
        public bool ShowInvalid{get;set;}
        
        public List<UserSettingsValidationFallback> DefaultFallbacks { get; set; }
    }
}



