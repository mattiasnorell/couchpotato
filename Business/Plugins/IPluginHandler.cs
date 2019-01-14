using CouchpotatoShared.Plugins;

namespace Couchpotato.Business.Plugins
{
    public interface IPluginHandler
    {
        void Register();
        void Run(PluginType pluginType);
    }
}