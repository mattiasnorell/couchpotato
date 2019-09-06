using System.Collections.Generic;
using Couchpotato.Business.Playlist.Models;

namespace Couchpotato.Business.Playlist
{
    public interface IPlaylistParser{
        Dictionary<string, PlaylistItem> Parse(string[] file);
    }
}