using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Core.Epg;
using Couchpotato.Core.Playlist;
using Couchpotato.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Couchpotato.Business.Plugins
{
    public class PluginHandler : IPluginHandler
    {
        private readonly string _pluginPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty, "plugins");

        private readonly Dictionary<PluginType, List<IPlugin>> _registeredPlugins = new();
        private readonly ILogging _logging;
        private readonly IFileHandler _fileHandler;
        private readonly IConfiguration _configuration;
        private readonly dynamic _pluginSettings;

        public PluginHandler(
            ILogging logging,
            IConfiguration configuration,
            IFileHandler fileHandler
        )
        {
            _logging = logging;
            _configuration = configuration;
            _fileHandler = fileHandler;

            var pluginPathSettings = _configuration.GetSection($"pluginPath")?.Value;
            if (!string.IsNullOrEmpty(pluginPathSettings))
            {
                _pluginPath = pluginPathSettings;
            }

            _pluginSettings = LoadPluginSettings();
        }

        private dynamic LoadPluginSettings()
        {
            var pluginConfigPath = _configuration.GetSection($"pluginConfigPath")?.Value;

            if (string.IsNullOrEmpty(pluginConfigPath))
            {
                pluginConfigPath = Path.Combine(_pluginPath, "config.json");
            }

            var config = _fileHandler.ReadTextFile(pluginConfigPath);

            return config != null ? JsonConvert.DeserializeObject<dynamic>(config) : new Dictionary<string, dynamic>();
        }

        public void Run(PluginType pluginType, PlaylistResult playlist = null, EpgResult epg = null)
        {
            if (!_registeredPlugins.ContainsKey(pluginType))
            {
                return;
            }

            _logging.Info($"\nRunning {pluginType}-plugins:");

            foreach (var plugin in _registeredPlugins[pluginType])
            {
                try
                {
                    _logging.Info($"- {plugin.GetType().Name}");
                    plugin.Run(playlist, epg);
                }
                catch (Exception e)
                {
                    _logging.Error($"Error running plugin {plugin.GetType().Name}. See log for more information.", e);
                }
            }
        }

        public void Register()
        {
            var pluginType = typeof(IPlugin);
            var pluginTypes = new List<Type>();
            var pluginsToActivate = new List<PluginToActivate>();

            if (!Directory.Exists(_pluginPath))
            {
                _logging.Warn($"Plugin folder {_pluginPath} not found");

                return;
            }

            var assemblies = Directory.GetFiles(_pluginPath, "*.dll").Select(Assembly.LoadFrom).ToList();

            foreach (var assembly in assemblies)
            {
                if (assembly == null)
                {
                    continue;
                }

                foreach (var assemblyType in assembly.GetTypes())
                {
                    if (assemblyType.IsInterface || assemblyType.IsAbstract)
                    {
                        continue;
                    }

                    if (pluginType.FullName != null && assemblyType.GetInterface(pluginType.FullName) != null)
                    {
                        pluginTypes.Add(assemblyType);
                    }
                }
            }

            foreach (var type in pluginTypes)
            {
                var attribute =
                    (CouchpotatoPluginAttribute)type.GetCustomAttribute(typeof(CouchpotatoPluginAttribute), false);

                if (attribute == null)
                {
                    continue;
                }

                var settings = GetSettings(type.Name);

                var requireSettings =
                    (RequireSettingsAttribute)type.GetCustomAttribute(typeof(RequireSettingsAttribute), false) != null;
                if (settings.Count == 0 && requireSettings)
                {
                    _logging.Info($"PluginHandler :: Can't load {type.Name}, settings not found");
                    continue;
                }

                pluginsToActivate.Add(new PluginToActivate(attribute.EventName, type, settings, attribute.Priority));
            }

            foreach (var pluginToActivate in pluginsToActivate.OrderBy(e => e.Priority))
            {
                var plugin = (IPlugin)Activator.CreateInstance(pluginToActivate.Type, pluginToActivate.Settings);

                if (!_registeredPlugins.ContainsKey(pluginToActivate.EventName))
                {
                    _registeredPlugins[pluginToActivate.EventName] = new List<IPlugin>();
                }

                _registeredPlugins[pluginToActivate.EventName].Add(plugin);

                _logging.Info($"PluginHandler :: Loaded {pluginToActivate.Type.Name}");
            }
        }

        private Dictionary<string, object> GetSettings(string key)
        {
            var pluginSettings = new Dictionary<string, dynamic>();

            if (!_pluginSettings.ContainsKey(key))
            {
                return pluginSettings;
            }

            var pluginSettingsValues = _pluginSettings[key];
            if (pluginSettingsValues == null) return pluginSettings;

            foreach (var setting in pluginSettingsValues)
            {
                pluginSettings.Add(setting.Name, setting.Value.Value);
            }

            return pluginSettings;
        }
    }

    public class PluginToActivate
    {
        public PluginToActivate(PluginType eventName, Type type, Dictionary<string, object> settings, int priority)
        {
            this.EventName = eventName;
            this.Type = type;
            this.Settings = settings;
            this.Priority = priority;
        }

        public Type Type { get; }
        public PluginType EventName { get; }
        public IPlugin Plugin { get; set; }
        public int Priority { get; }
        public Dictionary<string, object> Settings { get; }
    }
}