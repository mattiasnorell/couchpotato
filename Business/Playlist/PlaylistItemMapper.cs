using Couchpotato.Business.Settings;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistItemMapper: IPlaylistItemMapper{
        private readonly ISettingsProvider _settingsProvider;

        public PlaylistItemMapper(ISettingsProvider settingsProvider){
            _settingsProvider = settingsProvider;
        }

        public PlaylistItem Map(PlaylistItem playlistItem, UserSettingsStream stream){
            var channel = new PlaylistItem()
            {
                TvgName = playlistItem.TvgName,
                TvgId = stream.EpgId ?? playlistItem.TvgId,
                TvgLogo = stream.CustomLogo ?? playlistItem.TvgLogo,
                Url = playlistItem.Url,
                HasCustomLogo = String.Equals(stream.CustomLogo, playlistItem.TvgLogo, StringComparison.InvariantCulture)
            };

            if (!string.IsNullOrEmpty(stream.CustomGroupName) || !string.IsNullOrEmpty(_settingsProvider.DefaultGroup))
            {
                channel.GroupTitle = stream.CustomGroupName ?? _settingsProvider.DefaultGroup;
            }
            else
            {
                channel.GroupTitle = playlistItem.GroupTitle;
            }

            if (!string.IsNullOrEmpty(stream.FriendlyName))
            {
                channel.FriendlyName = stream.FriendlyName;
            }

            channel.Order = _settingsProvider.Streams.IndexOf(stream);

            return channel;
        }
    }
}