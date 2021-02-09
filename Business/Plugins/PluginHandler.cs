using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Core.Epg;
using Couchpotato.Core.Playlist;
using Couchpotato.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Couchpotato.Business.Plugins {
    public class PluginHandler : IPluginHandler {
        private string _pluginPath = Path.Combine (Path.GetDirectoryName (Assembly.GetEntryAssembly ().Location), "plugins");
        private Dictionary<PluginType, List<IPlugin>> _registeredPlugins = new Dictionary<PluginType, List<IPlugin>> ();
        private readonly ILogging _logging;
        private readonly IFileHandler _fileHandler;
        private readonly IConfiguration _configuration;
        private dynamic _pluginSettings;

        public PluginHandler (
            ILogging logging,
            IConfiguration configuration,
            IFileHandler fileHandler
        ) {
            _logging = logging;
            _configuration = configuration;
            _fileHandler = fileHandler;

            var pluginPathSettings = _configuration.GetSection ($"pluginPath")?.Value;
            if (!string.IsNullOrEmpty (pluginPathSettings)) {
                _pluginPath = pluginPathSettings;
            }

            _pluginSettings = LoadPluginSettings ();
        }

        public dynamic LoadPluginSettings () {
            var pluginConfigPath = _configuration.GetSection ($"pluginConfigPath")?.Value;

            if (string.IsNullOrEmpty (pluginConfigPath)) {
                pluginConfigPath = Path.Combine (_pluginPath, "config.json");
            }

            var config = _fileHandler.ReadTextFile (pluginConfigPath);

            if (config != null) {
                return JsonConvert.DeserializeObject<dynamic> (config);
            } else {
                return null;
            }
        }

        public void Run (PluginType pluginType, PlaylistResult playlist = null, EpgResult epg = null) {
            if (!_registeredPlugins.ContainsKey (pluginType)) {
                return;
            }

            _logging.Info ($"\nRunning {pluginType}-plugins:");

            foreach (var plugin in _registeredPlugins[pluginType]) {
                try {
                    _logging.Info ($"- {plugin.GetType().Name}");
                    plugin.Run (playlist, epg);
                } catch (Exception e) {
                    _logging.Error ($"Error running plugin {plugin.GetType().Name}", e);
                }
            }
        }

        public void Register () {
            var assemblies = new List<Assembly> ();
            var pluginType = typeof (IPlugin);
            var pluginTypes = new List<Type> ();

            if (!Directory.Exists (_pluginPath)) {
                _logging.Warn ($"Plugin folder {_pluginPath} not found");

                return;
            }

            var plugins = Directory.GetFiles (_pluginPath, "*.dll");

            foreach (var plugin in plugins) {
                var assembly = Assembly.LoadFrom (plugin);
                assemblies.Add (assembly);
            }

            foreach (var assembly in assemblies) {
                if (assembly == null) {
                    continue;
                }

                var types = assembly.GetTypes ();
                foreach (var assemblyType in types) {
                    if (assemblyType.IsInterface || assemblyType.IsAbstract) {
                        continue;
                    } else {
                        if (assemblyType.GetInterface (pluginType.FullName) != null) {
                            pluginTypes.Add (assemblyType);
                        }
                    }
                }
            }

            foreach (var type in pluginTypes) {
                var attribute = (CouchpotatoPluginAttribute) type.GetCustomAttribute (typeof (CouchpotatoPluginAttribute), false);

                if (attribute == null) {
                    continue;
                }

                var settings = GetSettings (type.Name);

                var requireSettings = (RequireSettingsAttribute) type.GetCustomAttribute (typeof (RequireSettingsAttribute), false) != null;
                if (settings.Count == 0 && requireSettings) {
                    _logging.Info ($"PluginHandler :: Can't load {type.Name}, settings not found");
                    continue;
                }

                var plugin = (IPlugin) Activator.CreateInstance (type, settings);

                if (!_registeredPlugins.ContainsKey (attribute.EventName)) {
                    _registeredPlugins[attribute.EventName] = new List<IPlugin> ();
                }

                _registeredPlugins[attribute.EventName].Add (plugin);

                _logging.Info ($"PluginHandler :: Loaded {type.Name}");
            }

            foreach(KeyValuePair<PluginType, List<IPlugin>> pluginCategory in _registeredPlugins){
                pluginCategory.Value.OrderBy(plugin => plugin, new PriorityComparer());
            }
        }

        private Dictionary<string, object> GetSettings (string key) {
            var pluginSettings = new Dictionary<string, dynamic> ();

            if (!_pluginSettings.ContainsKey (key)) {
                return pluginSettings;
            }

            var pluginSettingsValues = _pluginSettings[key];
            if (pluginSettingsValues == null) return pluginSettings;

            foreach (var setting in pluginSettingsValues) {
                pluginSettings.Add (setting.Name, setting.Value.Value);
            }

            return pluginSettings;
        }
    }

    public class PriorityComparer: IComparer<IPlugin>{
        public int Compare(IPlugin? x, IPlugin? y){
            var attributeX = (CouchpotatoPluginAttribute) x.GetCustomAttribute (typeof (CouchpotatoPluginAttribute), false);
            var attributeY = (CouchpotatoPluginAttribute) y.GetCustomAttribute (typeof (CouchpotatoPluginAttribute), false);

            return x.Priority < y.Priority ? -1 : 1;
        }
    }
}