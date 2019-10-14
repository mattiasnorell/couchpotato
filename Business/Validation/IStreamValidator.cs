using System.Collections.Generic;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation{
    public interface IStreamValidator{
        bool ValidateStreamByUrl(string url, string[] mediaTypes);
        bool ValidateSingleStream(PlaylistItem stream, string[] mediaTypes);
        void ValidateStreams(List<PlaylistItem> streams, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings);
    }
}