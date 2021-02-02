using Couchpotato.Core.Playlist;
using Couchpotato.Core.Plugins;
using Couchpotato.Core.Epg;
using System.Collections.Generic;

namespace Couchpotato.Business.Plugins
{
    public interface IPluginHandler
    {
        void Register();
        void Run(PluginType pluginType, PlaylistResult playlist = null, EpgResult epg = null);
    }
}