using System.Collections.Generic;
using Couchpotato.Business.Settings.Models;
using CouchpotatoShared.Channel;

namespace Couchpotato.Business.Playlist{
public interface IPlaylistProvider{
        ChannelResult GetChannels(string path, UserSettings settings);
        void Save(string path, List<Channel> channels);
    }
}