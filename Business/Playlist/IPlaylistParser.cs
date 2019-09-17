using System.Collections.Generic;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Playlist
{
    public interface IPlaylistParser{
        Dictionary<string, PlaylistItem> Parse(string[] file);
    }
}