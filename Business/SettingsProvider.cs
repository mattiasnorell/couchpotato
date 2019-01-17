using System;
using System.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Models;
using Newtonsoft.Json;

namespace Couchpotato.Business
{
    public class SettingsProvider :  ISettingsProvider
    {
        private readonly IFileHandler fileHandler;
        private readonly ILogging logging;

        public SettingsProvider(
            IFileHandler fileHandler,
            ILogging logging
        ){
            this.fileHandler = fileHandler;
            this.logging = logging;
        }

        public Settings Load(string path)
        {
            this.logging.Print("Loading settings from " + path);
            
            var file = this.fileHandler.GetSource(path);

            if(file == null){
                this.logging.Error($"- Couldn't load settingsfile from {path}");
                return null;
            }

            using (StreamReader responseReader = new StreamReader(file))
            {
                var response = responseReader.ReadToEnd();
                return JsonConvert.DeserializeObject<Settings>(response);
            }
        }
    }
}