using System.Collections.Generic;

namespace Couchpotato.Models
{
    public class SettingsGroup{
        public string GroupId{get;set;}
        public string FriendlyName{get;set;}
        public string[] Exclude{get;set;}
    }
}
