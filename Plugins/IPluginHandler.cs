using CouchpotatoShared.Plugins;

namespace Couchpotato.Plugins
{
    public interface IPluginHandler
    {
        void Register();
        void Run(PluginType pluginType);
    }
}