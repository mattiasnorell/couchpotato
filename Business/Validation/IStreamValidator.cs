using System.Collections.Generic;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation{
    public interface IStreamValidator{
        bool ValidateStreamByUrl(string url, string[] mediaTypes);
        bool ValidateSingleStream(PlaylistItem stream, string[] mediaTypes);
        List<string> ValidateStreams(List<PlaylistItem> streams, string[] mediaTypes);
    }
}