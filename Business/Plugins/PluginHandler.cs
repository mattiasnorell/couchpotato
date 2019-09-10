using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CouchpotatoShared.Plugins;
using Couchpotato.Business.Logging;
using Microsoft.Extensions.Configuration;
using CouchpotatoShared.Channel;
using CouchpotatoShared.Epg;

namespace Couchpotato.Business.Plugins
{
    public class PluginHandler : IPluginHandler
    {
        private string pluginPath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/plugins";
        private Dictionary<PluginType, List<IPlugin>> registeredPlugins = new Dictionary<PluginType, List<IPlugin>>();
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
                this.pluginPath = pluginPathSettings;
            }
        }

        public void Run(PluginType pluginType, ChannelResult streams = null, EpgList programGuide = null) {
            if(!this.registeredPlugins.ContainsKey(pluginType)){
                return;
            }

            _logging.Info($"\nRunning {pluginType}-plugins:");
            
            foreach(var plugin in this.registeredPlugins[pluginType]){
                try{
                    _logging.Info($"- {plugin.GetType().Name}");
                    plugin.Run(streams, programGuide);
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
            
            if(!Directory.Exists(pluginPath)){
                _logging.Warn("Plugin folder not found");
                
                return;
            }

            var plugins = Directory.GetFiles(pluginPath, "*.dll");

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

                if(!this.registeredPlugins.ContainsKey(attribute.EventName)){
                    this.registeredPlugins[attribute.EventName] = new List<IPlugin>();
                }

                registeredPlugins[attribute.EventName].Add(plugin);

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