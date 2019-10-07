using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Settings;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation
{
    public class StreamValidator : IStreamValidator
    {
        private readonly ILogging _logging;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IHttpClientWrapper _httpClientWrapper;

        public StreamValidator(
            ILogging logging,
            ISettingsProvider settingsProvider,
            IHttpClientWrapper httpClientWrapper
            )
        {
            _logging = logging;
            _settingsProvider = settingsProvider;
            _httpClientWrapper = httpClientWrapper;
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
            try
            {
                return _httpClientWrapper.Validate(url).Result;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}