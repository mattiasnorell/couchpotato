using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistItemMapper: IPlaylistItemMapper{
        public PlaylistItem Map(PlaylistItem playlistItem, UserSettingsStream stream, UserSettings settings){
            var channel = new PlaylistItem()
            {
                TvgName = playlistItem.TvgName,
                TvgId = stream.EpgId ?? playlistItem.TvgId,
                TvgLogo = playlistItem.TvgLogo,
                Url = playlistItem.Url
            };

            if (!string.IsNullOrEmpty(stream.CustomGroupName) || !string.IsNullOrEmpty(settings.DefaultGroup))
            {
                channel.GroupTitle = stream.CustomGroupName ?? settings.DefaultGroup;
            }
            else
            {
                channel.GroupTitle = playlistItem.GroupTitle;
            }

            if (!string.IsNullOrEmpty(stream.FriendlyName))
            {
                channel.FriendlyName = stream.FriendlyName;
            }

            channel.Order = settings.Streams.IndexOf(stream);

            return channel;
        }
    }
}