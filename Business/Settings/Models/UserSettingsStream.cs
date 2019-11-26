namespace Couchpotato.Business.Settings.Models
{
    public class UserSettingsStream{
        public string ChannelId{get;set;}
        public string CustomGroupName{get;set;}
        public string FriendlyName{get;set;}
        public string EpgId { get; set;}
        public string EpgTimeshift { get; set;}
        public string CustomLogo{get;set;}
        public string[] Fallbacks { get; set; }
    }
}



