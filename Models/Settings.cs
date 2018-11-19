using System.Collections.Generic;

namespace Couchpotato.Models
{
    public class Settings{
        public string OutputPath {get;set;} 
        public string M3uPath{get;set;} 
        public bool Compress{get;set;}
        public bool ValidateStreams{ get; set; }
        public List<SettingsFallbackChannel> DefaultChannelFallbacks{get;set;}
        public string[] EpgPath{get;set;}
        public string DefaultChannelGroup{get;set;}
        public List<SettingsChannel> Channels{get;set;}
        public List<SettingsGroup> Groups {get;set;}
    }
}
