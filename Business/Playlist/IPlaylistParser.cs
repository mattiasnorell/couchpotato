using System.Collections.Generic;
using Couchpotato.Business.Playlist.Models;

namespace Couchpotato.Business.Playlist
{
    public interface IPlaylistParser{
        List<PlaylistItem> Parse(string[] file);
    }
}