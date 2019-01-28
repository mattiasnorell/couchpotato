using CouchpotatoShared.Channel;
using CouchpotatoShared.Plugins;

namespace Couchpotato.Business.Plugins
{
    public interface IPluginHandler
    {
        void Register();
        void Run(PluginType pluginType, ChannelResult channelResult = null);
    }
}