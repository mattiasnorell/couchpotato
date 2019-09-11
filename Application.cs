
using System;
using System.Linq;
using Couchpotato.Business;
using Couchpotato.Business.Playlist;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Plugins;
using CouchpotatoShared.Plugins;
using Couchpotato.Business.Settings;
using Couchpotato.Business.IO;

namespace Couchpotato {
    class Application: IApplication{
        private readonly ISettingsProvider _settingsProvider;
        private readonly IPlaylistProvider _playlistProvider;
        private readonly IEpgProvider _epgProvider;
        private readonly ICompression _compression;
        private readonly IPluginHandler _pluginHandler;
        private readonly ILogging _logging;
        private readonly IFileHandler _fileHandler;

        public Application(
            ISettingsProvider settingsProvider, 
            IPlaylistProvider playlistProvider, 
            IEpgProvider epgProvider, 
            ICompression compression,
            IPluginHandler pluginHandler,
            ILogging logging,
            IFileHandler fileHandler
        ){
            _playlistProvider = playlistProvider;
            _settingsProvider = settingsProvider;
            _epgProvider = epgProvider;
            _compression = compression;
            _pluginHandler = pluginHandler;
            _logging = logging;
            _fileHandler = fileHandler;
        }

        public void Run(string[] settingsPaths){
            
            if(settingsPaths == null || settingsPaths.Length == 0){
                _logging.Error($"No settings file(s) found. Please fix.");
                
                Environment.Exit(0);
            }

            var startTime = DateTime.Now;
            
            _pluginHandler.Register();

            _pluginHandler.Run(PluginType.ApplicationStart);
            
            foreach(var path in settingsPaths){
                if(string.IsNullOrEmpty(path) || !path.ToLower().Contains(".json")){
                    _logging.Error($"Settings parameter \"{path}\" isn't valid.");
                    
                    continue;
                }

                Create(path);
            }

            var endTime = DateTime.Now;
            var timeTaken = (endTime - startTime).TotalSeconds;

            _pluginHandler.Run(PluginType.ApplicationFinished);
            
            _logging.Print($"\nDone! It took {Math.Ceiling(timeTaken)} seconds.");
        }

        private void Create(string settingsPath){
            var settings = _settingsProvider.Load(settingsPath);

            if(settings == null){
                _logging.Info($"\nNeed settings. Please fix. Thanks.");
                return;
            }

            _pluginHandler.Run(PluginType.BeforeChannel);
            var channelResult = _playlistProvider.GetPlaylist(settings.M3uPath, settings);
            _pluginHandler.Run(PluginType.AfterChannel, channelResult);

            if(!channelResult.Channels.Any()){
               _logging.Info($"\nNo channels found so no reason to continue. Bye bye.");
                
                return;
            }

            _pluginHandler.Run(PluginType.BeforeEpg, channelResult);
            var epgFile = _epgProvider.GetProgramGuide(settings.EpgPath, settings);
            _pluginHandler.Run(PluginType.AfterEpg, channelResult, epgFile);

            var outputPath = settings.OutputPath ?? "./";
            var folderExist = _fileHandler.CheckIfFolderExist(outputPath, true);

            if(!folderExist){
                _logging.Info($"\nNo output folder found. Can't continue.");
                return;
            }

            var outputM3uPath = _playlistProvider.Save(outputPath, "channels.m3u", channelResult.Channels);
            var outputEpgPath = _epgProvider.Save(outputPath, "epg.xml", epgFile);
            

            if(settings.Compress){
                _compression.Compress(outputM3uPath);
                _compression.Compress(outputEpgPath);
            }
        }
    }
}