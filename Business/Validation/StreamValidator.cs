using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation
{
    public class StreamValidator : IStreamValidator
    {
        private readonly ILogging _logging;
        private readonly IHttpClientWrapper _httpClientWrapper;

        public StreamValidator(
            ILogging logging,
            IHttpClientWrapper httpClientWrapper
            )
        {
            _logging = logging;
            _httpClientWrapper = httpClientWrapper;
        }

        public bool ValidateStreamByUrl(string url, string[] mediaTypes)
        {
            return CheckAvailability(url,mediaTypes);
        }

        public bool ValidateSingleStream(PlaylistItem stream, string[] mediaTypes)
        {
            return CheckAvailability(stream.Url, mediaTypes);
        }

        public List<String> ValidateStreams(List<PlaylistItem> streams, string[] mediaTypes)
        {
            var streamCount = streams.Count();
            var i = 0;
            var invalidStreams = new List<string>();

            foreach (var stream in streams.ToList())
            {
                if (!CheckAvailability(stream.Url, mediaTypes))
                {
                    invalidStreams.Add(stream.TvgName);
                    streams.Remove(stream);
                }

                i++;

                _logging.Progress("- Progress", i, streamCount);
            }

            return invalidStreams;
        }

        private bool CheckAvailability(string url, string[] mediaTypes)
        {
            try
            {
                return _httpClientWrapper.Validate(url, mediaTypes).Result;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}