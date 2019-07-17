using CouchpotatoShared.Channel;
using CouchpotatoShared.Plugins;
using CouchpotatoShared.Epg;

namespace Couchpotato.Business.Plugins
{
    public interface IPluginHandler
    {
        void Register();
        void Run(PluginType pluginType, ChannelResult channelResult = null, EpgList epgList = null);
    }
}