using System.Collections.Generic;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation{
    public interface IStreamValidator{
        bool ValidateStreamByUrl(string url);
        bool ValidateSingleStream(PlaylistItem stream);
        void ValidateStreams(List<PlaylistItem> streams, Dictionary<string, PlaylistItem> playlistItems);
        PlaylistItem GetSourceFallback(string id, Dictionary<string, PlaylistItem> channels);
    }
}