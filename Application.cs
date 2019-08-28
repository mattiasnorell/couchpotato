
using System;
using System.IO;
using System.Linq;
using Couchpotato.Business;
using Couchpotato.Business.Playlist;
using Couchpotato.Business.Compression;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Plugins;
using CouchpotatoShared.Plugins;
using Couchpotato.Business.IO;

namespace Couchpotato {
    class Application: IApplication{
        private readonly ISettingsProvider settingsProvider;
        private readonly IPlaylistProvider playlistProvider;
        private readonly IEpgProvider epgProvider;
        private readonly ICompression compression;
        private readonly IPluginHandler pluginHandler;
        private readonly ILogging logging;

        public Application(
            ISettingsProvider settingsProvider, 
            IPlaylistProvider playlistProvider, 
            IEpgProvider epgProvider, 
            ICompression compression,
            IPluginHandler pluginHandler,
            ILogging logging
        ){
            this.settingsProvider = settingsProvider;
            this.playlistProvider = playlistProvider;
            this.epgProvider = epgProvider;
            this.compression = compression;
            this.pluginHandler = pluginHandler;
            this.logging = logging;
        }

        public void Run(string[] settingsPaths){
            
            if(settingsPaths == null || settingsPaths.Length == 0){
                this.logging.Error($"No settings file(s) found. Please fix.");
                
                Environment.Exit(0);
            }

            var startTime = DateTime.Now;
            
            this.pluginHandler.Register();

            this.pluginHandler.Run(PluginType.ApplicationStart);
            
            foreach(var path in settingsPaths){
                if(string.IsNullOrEmpty(path) || !path.ToLower().Contains(".json")){
                    this.logging.Error($"Settings parameter \"{path}\" isn't valid.");
                    
                    continue;
                }

                Create(path);
            }

            var endTime = DateTime.Now;
            var timeTaken = (endTime - startTime).TotalSeconds;

            this.pluginHandler.Run(PluginType.ApplicationFinished);
            
            this.logging.Print($"\nDone! It took {Math.Ceiling(timeTaken)} seconds.");
        }

        private void Create(string settingsPath){
            var settings = settingsProvider.Load(settingsPath);

            if(settings == null){
                this.logging.Info($"\nNeed settings. Please fix. Thanks.");
                return;
            }

            this.pluginHandler.Run(PluginType.BeforeChannel);
            var channelResult = playlistProvider.GetPlaylist(settings.M3uPath, settings);
            this.pluginHandler.Run(PluginType.AfterChannel, channelResult);

            if(!channelResult.Channels.Any()){
               this.logging.Info($"\nNo channels found so no reason to continue. Bye bye.");
                
                Environment.Exit(0);
            }

            this.pluginHandler.Run(PluginType.BeforeEpg, channelResult);
            var epgFile = epgProvider.GetProgramGuide(settings.EpgPath, settings);
            this.pluginHandler.Run(PluginType.AfterEpg, channelResult, epgFile);

            var outputPath = settings.OutputPath ?? "./";
        
            if(!Directory.Exists(outputPath)){
                this.logging.Info($"Couldn't find output folder, creating it at {outputPath}!");
                Directory.CreateDirectory(outputPath);
            }

            var outputM3uPath = Path.Combine(outputPath, "channels.m3u");
            playlistProvider.Save(outputM3uPath, channelResult.Channels);

            var outputEpgPath = Path.Combine(outputPath, "epg.xml");
            epgProvider.Save(outputEpgPath, epgFile);
            

            if(settings.Compress){
                compression.Compress(outputM3uPath);
                compression.Compress(outputEpgPath);
            }
        }
    }
}