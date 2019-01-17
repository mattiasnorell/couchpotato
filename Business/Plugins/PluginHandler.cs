using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CouchpotatoShared.Plugins;
using Couchpotato.Business.Logging;

namespace Couchpotato.Business.Plugins
{
    public class PluginHandler : IPluginHandler
    {
        private string pluginPath = @"C:\Projects\couchpotato-plugins\bin\Release\netcoreapp2.1\win10-x64\publish"; //$"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/plugins";
        private List<Assembly> assemblies = new List<Assembly>();
        private Dictionary<PluginType, List<IPlugin>> registeredPlugins = new Dictionary<PluginType, List<IPlugin>>();
        private readonly ILogging logging;

        public PluginHandler(
            ILogging logging
        ){
            this.logging = logging;
        }

        public void Run(PluginType pluginType) {
            if(!this.registeredPlugins.ContainsKey(pluginType)){
                return;
            }

            foreach(var plugin in this.registeredPlugins[pluginType]){

                try{
                    plugin.Run();
                }catch (Exception e)
                {
                    this.logging.Error($"Error running plugin {pluginType}", e);
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
                var plugin = (IPlugin)Activator.CreateInstance(type);
                var attribute = (CouchpotatoPluginAttribute)type.GetCustomAttribute(typeof(CouchpotatoPluginAttribute), false);

                if(attribute == null){
                    continue;
                }

                if(!this.registeredPlugins.ContainsKey(attribute.EventName)){
                    this.registeredPlugins[attribute.EventName] = new List<IPlugin>();
                }

                registeredPlugins[attribute.EventName].Add(plugin);

                this.logging.Info($"PluginHandler :: Loaded {type}");
            }
        }
    }
}