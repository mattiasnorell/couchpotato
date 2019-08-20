using System.Collections.Generic;
using CouchpotatoShared.Channel;

namespace Couchpotato.Business.Validation{
    public interface IStreamValidator{
        bool ValidateStreamByUrl(string url);
        bool ValidateSingleStream(Channel stream);
        List<string> ValidateStreams(List<Channel> streams);
    }
}