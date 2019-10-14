using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Playlist;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation
{
    public class StreamValidator : IStreamValidator
    {
        private readonly ILogging _logging;
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly IPlaylistItemMapper _playlistItemMapper;

        public StreamValidator(
            ILogging logging,
            IHttpClientWrapper httpClientWrapper,
            IPlaylistItemMapper playlistItemMapper
            )
        {
            _logging = logging;
            _httpClientWrapper = httpClientWrapper;
            _playlistItemMapper = playlistItemMapper;
        }

        public void ValidateStreams(List<PlaylistItem> streams, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            _logging.Print("\nValidating streams. This might disconnect all active streams.");
            Validate(streams, settings.Validation.ContentTypes);

            if (!streams.Any(e => !e.IsValid))
            {
                return;
            }

            _logging.Info("\nBroken streams found, trying to find fallback channels");

            foreach (var invalidStream in streams.Where(e => !e.IsValid))
            {
                var fallbackStream = GetFallbackStream(invalidStream.TvgName, playlistItems, settings);

                if (fallbackStream != null)
                {
                    _logging.Info($"- Fallback found for {invalidStream.TvgName}, now using {fallbackStream.TvgName}");
                    streams.Add(fallbackStream);
                }
                else
                {
                    _logging.Warn($"- Sorry, no fallback found for {invalidStream.TvgName}");
                }
            }

            // Temporary workaround
            if (!settings.Validation.ShowInvalid){
                streams = streams.Where(e => e.IsValid).ToList();
            }
        }

        public bool ValidateStreamByUrl(string url, string[] mediaTypes)
        {
            return CheckAvailability(url, mediaTypes);
        }

        public bool ValidateSingleStream(PlaylistItem stream, string[] mediaTypes)
        {
            return CheckAvailability(stream.Url, mediaTypes);
        }

        private void Validate(List<PlaylistItem> streams, string[] mediaTypes)
        {
            var streamCount = streams.Count();
            var i = 0;

            foreach (var stream in streams.ToList())
            {
                if (!CheckAvailability(stream.Url, mediaTypes))
                {
                    stream.IsValid = false;
                }

                i++;

                _logging.Progress("- Progress", i, streamCount);
            }
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



        private PlaylistItem GetFallbackStream(string tvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            var specificFallback = GetSpecificFallback(tvgName, playlistItems, settings);
            if (specificFallback != null)
            {
                return specificFallback;
            }

            var fallbackStream = GetDefaultFallback(tvgName, playlistItems, settings);
            if (fallbackStream != null)
            {
                return fallbackStream;
            }

            return null;
        }

        private PlaylistItem GetDefaultFallback(string originalTvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {

            if (settings.Validation.DefaultFallbacks == null)
            {
                return null;
            }

            var tvgNames = settings.Validation.DefaultFallbacks.FirstOrDefault(e => originalTvgName.Contains(e.Key));


            if (tvgNames == null || tvgNames.Value == null)
            {
                return null;
            }

            foreach (var tvgName in tvgNames.Value)
            {
                var fallbackTvgName = originalTvgName.Replace(tvgNames.Key, tvgName);

                if (!playlistItems.ContainsKey(fallbackTvgName))
                {
                    continue;
                }

                var fallback = playlistItems[fallbackTvgName];
                var isValid = ValidateStreamByUrl(fallback.Url, settings.Validation.ContentTypes);
                if (!isValid)
                {
                    continue;
                }

                var channelSetting = settings.Streams.FirstOrDefault(e => e.ChannelId == originalTvgName);
                return _playlistItemMapper.Map(fallback, channelSetting, settings);
            };

            return null;
        }

        private PlaylistItem GetSpecificFallback(string tvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            var channelSetting = settings.Streams.FirstOrDefault(e => e.ChannelId == tvgName);
            if (channelSetting == null || channelSetting.Fallbacks == null)
            {
                return null;
            }

            foreach (var fallbackChannelId in channelSetting.Fallbacks)
            {

                if (!playlistItems.ContainsKey(fallbackChannelId))
                {
                    continue;
                }

                var fallbackChannel = playlistItems[fallbackChannelId];
                if (fallbackChannel == null)
                {
                    continue;
                }

                var isValid = ValidateStreamByUrl(fallbackChannel.Url, settings.Validation.ContentTypes);

                if (isValid)
                {
                    return _playlistItemMapper.Map(fallbackChannel, channelSetting, settings);
                }

            };

            return null;
        }

    }
}