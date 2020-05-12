using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Couchpotato.Core.Plugins;
using Couchpotato.Business.Logging;
using Microsoft.Extensions.Configuration;
using Couchpotato.Core.Playlist;
using Couchpotato.Core.Epg;

namespace Couchpotato.Business.Plugins
{
    public class PluginHandler : IPluginHandler
    {
        private string _pluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "plugins");
        private Dictionary<PluginType, List<IPlugin>> _registeredPlugins = new Dictionary<PluginType, List<IPlugin>>();
        private readonly ILogging _logging;
        private readonly IConfiguration _configuration;

        public PluginHandler(
            ILogging logging,
            IConfiguration configuration
        ){
            _logging = logging;
            _configuration = configuration;

            var pluginPathSettings = _configuration.GetSection($"pluginPath")?.Value;
            if(!string.IsNullOrEmpty(pluginPathSettings)){
                _pluginPath = pluginPathSettings;
            }
        }

        public void Run(PluginType pluginType, List<PlaylistItem> playlistItems = null, EpgList programGuide = null) {
            if(!_registeredPlugins.ContainsKey(pluginType)){
                return;
            }

            _logging.Info($"\nRunning {pluginType}-plugins:");
            
            foreach(var plugin in _registeredPlugins[pluginType]){
                try{
                    _logging.Info($"- {plugin.GetType().Name}");
                    plugin.Run(playlistItems, programGuide);
                }catch (Exception e)
                {
                    _logging.Error($"Error running plugin {plugin.GetType().Name}", e);
                }
            }
        }

        public void Register()
        {
            var assemblies = new List<Assembly>();
            var pluginType = typeof(IPlugin);
            var pluginTypes = new List<Type>();
            
            if(!Directory.Exists(_pluginPath)){
                _logging.Warn($"Plugin folder {_pluginPath} not found");
                
                return;
            }

            var plugins = Directory.GetFiles(_pluginPath, "*.dll");

            foreach(var plugin in plugins){
                var assembly = Assembly.LoadFrom(plugin);
                assemblies.Add(assembly);
            }

            foreach(var assembly in assemblies){
                if(assembly == null){
                    continue;
                }

                var types = assembly.GetTypes();
                foreach(var assemblyType in types){
                    if(assemblyType.IsInterface || assemblyType.IsAbstract){
                        continue;
                    }else{
                        if(assemblyType.GetInterface(pluginType.FullName) != null){
                            pluginTypes.Add(assemblyType);
                        }
                    }
                }
            }

            foreach(var type in pluginTypes){
                var attribute = (CouchpotatoPluginAttribute)type.GetCustomAttribute(typeof(CouchpotatoPluginAttribute), false);

                if(attribute == null){
                    continue;
                }

                var settings = GetSettings(type.Name);
                
                var requireSettings = (RequireSettingsAttribute)type.GetCustomAttribute(typeof(RequireSettingsAttribute), false) != null;
                if(settings.Count == 0 && requireSettings){
                    _logging.Info($"PluginHandler :: Can't load {type.Name}, settings not found");
                    continue;
                }

                var plugin = (IPlugin)Activator.CreateInstance(type, settings);

                if(!_registeredPlugins.ContainsKey(attribute.EventName)){
                    _registeredPlugins[attribute.EventName] = new List<IPlugin>();
                }

                _registeredPlugins[attribute.EventName].Add(plugin);

                _logging.Info($"PluginHandler :: Loaded {type.Name}");
            }
        }
        
        private Dictionary<string, object> GetSettings(string key){
            var pluginSettingsValues = _configuration.GetSection($"plugins:{key}")?.GetChildren();
            var pluginSettings = new Dictionary<string, object>();

            if(pluginSettingsValues == null) return null;

            foreach(var setting in pluginSettingsValues){
                pluginSettings.Add(setting.Key, setting.Value);
            }
                        
            return pluginSettings;
        }
    }
}