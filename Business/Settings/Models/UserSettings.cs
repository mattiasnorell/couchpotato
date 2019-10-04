using System;
using System.Collections.Generic;

namespace Couchpotato.Business.Settings.Models
{
    public class UserSettings
    {

        public UserSettings()
        {
            this.Validation = new UserSettingsValidation()
            {
                Enabled = false
            };
        }

        public string OutputPath { get; set; }
        public string M3uPath { get; set; }
        public bool Compress { get; set; }
        public UserSettingsValidation Validation { get; set; }
        public List<UserSettingsFallbackChannel> DefaultStreamFallbacks { get; set; }
        public UserSettingsEpg Epg { get; set; }
        public string DefaultGroup { get; set; }
        public List<UserSettingsStream> Streams { get; set; }
        public List<UserSettingsGroup> Groups { get; set; }
    }
}
