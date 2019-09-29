using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Settings;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation
{
    public class StreamValidator : IStreamValidator
    {
        private readonly ILogging _logging;
        private readonly ISettingsProvider _settingsProvider;

        public StreamValidator(
            ILogging logging,
            ISettingsProvider settingsProvider
            )
        {
            _logging = logging;
            _settingsProvider = settingsProvider;
        }

        public bool ValidateStreamByUrl(string url)
        {
            return CheckAvailability(url);
        }

        public bool ValidateSingleStream(PlaylistItem stream)
        {
            return CheckAvailability(stream.Url);
        }

        public List<String> ValidateStreams(List<PlaylistItem> streams)
        {
            var streamCount = streams.Count();
            var i = 0;
            var invalidStreams = new List<string>();

            foreach (var stream in streams.ToList())
            {
                if (!CheckAvailability(stream.Url))
                {
                    invalidStreams.Add(stream.TvgName);
                    streams.Remove(stream);
                }

                i++;

                _logging.Progress("- Progress", i, streamCount);
            }

            return invalidStreams;
        }

        private bool CheckAvailability(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";

            try
            {
                using (var response = (WebResponse)request.GetResponse())
                {
                    // As mentioned in the settings provider, the get() isn't really a great solution but it works for now
                    if (!_settingsProvider.Get().Validation.ContentTypes.Any(e => e == response.ContentType))
                    {
                        return false;
                    }

                    request.Abort();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}