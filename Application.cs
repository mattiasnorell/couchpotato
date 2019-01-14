
using System;
using System.IO;
using System.Linq;
using Couchpotato.Business;
using Couchpotato.Plugins;

namespace Couchpotato {
    class Application: IApplication{
        private readonly ISettingsProvider settingsProvider;
        private readonly IChannelProvider channelProvider;
        private readonly IEpgProvider epgProvider;
        private readonly ICompression compression;
        private readonly IPluginHandler pluginHandler;

        public Application(
            ISettingsProvider settingsProvider, 
            IChannelProvider channelProvider, 
            IEpgProvider epgProvider, 
            ICompression compression,
            IPluginHandler pluginHandler
        ){
            this.settingsProvider = settingsProvider;
            this.channelProvider = channelProvider;
            this.epgProvider = epgProvider;
            this.compression = compression;
            this.pluginHandler = pluginHandler;
        }

        public void Run(string[] settingsPaths){
            var startTime = DateTime.Now;
            
            this.pluginHandler.Register();

            this.pluginHandler.RunPlugins(PluginType.ApplicationStart);
            
            foreach(var path in settingsPaths){
                if(string.IsNullOrEmpty(path) || !path.ToLower().Contains(".json")){
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Settings parameter \"{path}\" isn't valid.");
                    Console.ForegroundColor = ConsoleColor.White;
                    
                    continue;
                }

                Create(path);
            }

            var endTime = DateTime.Now;
            var timeTaken = (endTime - startTime).TotalSeconds;

            this.pluginHandler.RunPlugins(PluginType.ApplicationFinished);
            Console.WriteLine($"\nDone! It took {Math.Ceiling(timeTaken)} seconds.");
        }

        private void Create(string settingsPath){
            var settings = settingsProvider.Load(settingsPath);

            if(settings == null){
                Console.WriteLine($"\nNeed settings. Please fix. Thanks.");
                return;
            }

            this.pluginHandler.RunPlugins(PluginType.BeforeChannel);
            var channelResult = channelProvider.GetChannels(settings.M3uPath, settings);

            if(!channelResult.Channels.Any()){
                Console.WriteLine($"\nNo channels found so no reason to continue. Bye bye.");
                
                Environment.Exit(0);
            }

            this.pluginHandler.RunPlugins(PluginType.BeforeEpg);

            var epgFile = epgProvider.Load(settings.EpgPath, settings);
            var outputPath = settings.OutputPath ?? "./";
        
            if(!Directory.Exists(outputPath)){
                Console.WriteLine($"Couldn't find output folder, creating it at {outputPath}!");
                Directory.CreateDirectory(outputPath); 
            }

            var outputM3uPath = Path.Combine(outputPath, "channels.m3u");
            channelProvider.Save(outputM3uPath, channelResult.Channels);

            var outputEpgPath = Path.Combine(outputPath, "epg.xml");
            epgProvider.Save(outputEpgPath, epgFile);
            

            if(settings.Compress){
                compression.Compress(outputM3uPath);
                compression.Compress(outputEpgPath);
            }
        }
    }
}