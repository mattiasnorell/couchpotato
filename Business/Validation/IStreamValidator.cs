using System.Collections.Generic;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation{
    public interface IStreamValidator{
        bool ValidateStreamByUrl(string url);
        bool ValidateSingleStream(PlaylistItem stream);
        List<string> ValidateStreams(List<PlaylistItem> streams);
    }
}