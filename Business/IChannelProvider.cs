using System.Collections.Generic;
using Couchpotato.Models;

public interface IChannelProvider{
    List<Channel> Load(string path, Settings settings);
    void Save(string path, List<Channel> channels);
}