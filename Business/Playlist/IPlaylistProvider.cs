using System.Collections.Generic;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Playlist{
    public interface IPlaylistProvider{
        List<PlaylistItem> GetPlaylist();
        string Save(string path, string fileName, List<PlaylistItem> channels);
    }
}