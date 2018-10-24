using System.Collections.Generic;

namespace M3uFixer.Models
{
    public class Settings{
        public string OutputPath {get;set;} 
        public string M3uPath{get;set;} 
        public bool Gzip{get;set;}
        public string[] EpgPath{get;set;}
        public string DefaultChannelGroup{get;set;}
        public List<SettingsChannel> Channels{get;set;}
        public List<SettingsGroup> Groups {get;set;}
    }
}
