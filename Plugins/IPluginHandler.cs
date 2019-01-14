namespace Couchpotato.Plugins
{
    public interface IPluginHandler
    {
        void Register();
        void RunPlugins(PluginType pluginType);
    }
}