using System.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Business.IO;
using Newtonsoft.Json;
using Couchpotato.Business.Settings.Models;

namespace Couchpotato.Business.Settings
{
    public class SettingsProvider :  ISettingsProvider
    {
        private readonly IFileHandler _fileHandler;
        private readonly ILogging _logging;

        private UserSettings _settings = null;

        public SettingsProvider(
            IFileHandler fileHandler,
            ILogging logging
        ){
            _fileHandler = fileHandler;
            _logging = logging;
        }

        public UserSettings Load(string path)
        {
            _logging.Print("Loading settings from " + path);
            
            var file = _fileHandler.GetSource(path);

            if(file == null){
                _logging.Error($"- Couldn't load settingsfile from {path}");
                return null;
            }

            using (var responseReader = new StreamReader(file))
            {
                var response = responseReader.ReadToEnd();
                _settings = JsonConvert.DeserializeObject<UserSettings>(response);

                return _settings;
            }
        }
    }
}