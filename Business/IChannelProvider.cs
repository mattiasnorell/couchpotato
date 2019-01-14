using System.Collections.Generic;
using Couchpotato.Models;
using CouchpotatoShared.Channel;

namespace Couchpotato.Business{
public interface IChannelProvider{
        ChannelResult GetChannels(string path, Settings settings);
        void Save(string path, List<Channel> channels);
    }
}