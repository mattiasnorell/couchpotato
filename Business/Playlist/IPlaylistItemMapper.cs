using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Playlist
{
    public interface IPlaylistItemMapper{
        PlaylistItem Map(PlaylistItem playlistItem, UserSettingsStream stream);
    }
}