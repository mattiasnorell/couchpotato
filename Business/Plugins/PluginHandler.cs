using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CouchpotatoShared.Plugins;
using Couchpotato.Business.Logging;
using Microsoft.Extensions.Configuration;

namespace Couchpotato.Business.Plugins
{
    public class PluginHandler : IPluginHandler
    {
        private string pluginPath = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/plugins";
        private List<Assembly> assemblies = new List<Assembly>();
        private Dictionary<PluginType, List<IPlugin>> registeredPlugins = new Dictionary<PluginType, List<IPlugin>>();
        private readonly ILogging logging;
        private readonly IConfiguration configuration;

        public PluginHandler(
            ILogging logging,
            IConfiguration configuration
        ){
            this.logging = logging;
            this.configuration = configuration;

            var pluginPathSettings = this.configuration.GetSection($"pluginPath")?.Value;
            if(!string.IsNullOrEmpty(pluginPathSettings)){
                this.pluginPath = pluginPathSettings;
            }
        }

        public void Run(PluginType pluginType) {
            if(!this.registeredPlugins.ContainsKey(pluginType)){
                return;
            }

            this.logging.Info($"\nRunning {pluginType} plugins:");
            foreach(var plugin in this.registeredPlugins[pluginType]){
                try{
                    this.logging.Info($"- {plugin.GetType().Name}");
                    plugin.Run();
                }catch (Exception e)
                {
                    this.logging.Error($"Error running plugin {plugin.GetType().Name}", e);
                }
            }
        }

        public void Register()
        {
            var pluginType = typeof(IPlugin);
            var pluginTypes = new List<Type>();
            
            if(!Directory.Exists(pluginPath)){
                return;
            }

            var plugins = Directory.GetFiles(pluginPath, "*.dll");

            foreach(var plugin in plugins){

                if(!File.Exists(plugin)){
                    this.logging.Info($"PluginHandler :: Plugin {plugin} not found");
                    continue;
                }

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
                var settings = GetSettings(type.Name);
                var plugin = (IPlugin)Activator.CreateInstance(type, settings);
                var attribute = (CouchpotatoPluginAttribute)type.GetCustomAttribute(typeof(CouchpotatoPluginAttribute), false);

                if(attribute == null){
                    continue;
                }


                if(!this.registeredPlugins.ContainsKey(attribute.EventName)){
                    this.registeredPlugins[attribute.EventName] = new List<IPlugin>();
                }

                registeredPlugins[attribute.EventName].Add(plugin);

                this.logging.Info($"PluginHandler :: Loaded {type.Name}");
            }
        }
        

        private Dictionary<string, object> GetSettings(string key){
            var pluginSettingsValues = this.configuration.GetSection($"plugins:{key}")?.GetChildren();
            var pluginSettings = new Dictionary<string, object>();

            if(pluginSettings != null){
                foreach(var setting in pluginSettingsValues){
                    pluginSettings.Add(setting.Key, setting.Value);
                }
            }
            
            return pluginSettings;
        }
    }
}