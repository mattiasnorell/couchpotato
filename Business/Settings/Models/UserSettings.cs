using System.Collections.Generic;

namespace Couchpotato.Business.Settings.Models
{
    public class UserSettings{
        public string OutputPath {get;set;} 
        public string M3uPath{get;set;} 
        public bool Compress{get;set;}
        public bool ValidateStreams{ get; set; }
        public List<UserSettingsFallbackChannel> DefaultChannelFallbacks{get;set;}
        public string[] EpgPath{get;set;}
        public string DefaultChannelGroup{get;set;}
        public List<UserSettingsChannel> Channels{get;set;}
        public List<UserSettingsGroup> Groups {get;set;}
    }
}
