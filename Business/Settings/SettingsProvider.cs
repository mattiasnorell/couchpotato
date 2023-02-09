using System.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Business.IO;
using Newtonsoft.Json;
using Couchpotato.Business.Settings.Models;
using System.Collections.Generic;

namespace Couchpotato.Business.Settings
{
    public class SettingsProvider : ISettingsProvider
    {
        private readonly IFileHandler _fileHandler;
        private readonly ILogging _logging;

        private UserSettings _settings = null;

        public SettingsProvider(
            IFileHandler fileHandler,
            ILogging logging
        )
        {
            _fileHandler = fileHandler;
            _logging = logging;
        }

        public string Source => _settings?.M3uPath ?? null;
        public string DefaultGroup => _settings.DefaultGroup;
        public string OutputPath => _settings.OutputPath;
        public string OutputFilename => _settings.OutputFilename ?? null;
        public bool Compress => _settings.Compress;
        public UserSettingsEpg Epg => _settings.Epg;
        public List<UserSettingsStream> Streams => _settings.Streams;
        public List<UserSettingsGroup> Groups => _settings.Groups;
        public UserSettingsValidation Validation => _settings.Validation;
        public int PlaylistCacheDuration => _settings.PlaylistCacheDuration;

        public bool Load(string path)
        {
            _logging.Print("Loading settings from " + path);

            var file = _fileHandler.GetSource(path);

            if (file == null)
            {
                _logging.Error($"- Couldn't load settings file from {path}");
                return false;
            }

            using var responseReader = new StreamReader(file);
            var response = responseReader.ReadToEnd();
            _settings = JsonConvert.DeserializeObject<UserSettings>(response);

            return _settings != null;
        }
    }
}