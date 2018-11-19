using System.Collections.Generic;
using Couchpotato.Models;

namespace Couchpotato.Business{
    public interface IStreamValidator{
        bool ValidateStreamByUrl(string url);
        bool ValidateSingleStream(Channel stream);
        List<string> ValidateStreams(List<Channel> streams);
    }
}