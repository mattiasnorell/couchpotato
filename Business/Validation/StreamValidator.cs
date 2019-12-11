using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.IO;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Playlist;
using Couchpotato.Business.Settings;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Validation
{
    public class StreamValidator : IStreamValidator
    {
        private readonly ILogging _logging;
        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly IPlaylistItemMapper _playlistItemMapper;
        private readonly ISettingsProvider _settingsProvider;

        public StreamValidator(
            ILogging logging,
            IHttpClientWrapper httpClientWrapper,
            IPlaylistItemMapper playlistItemMapper,
            ISettingsProvider settingsProvider
            )
        {
            _logging = logging;
            _httpClientWrapper = httpClientWrapper;
            _playlistItemMapper = playlistItemMapper;
            _settingsProvider = settingsProvider;
        }

        public void ValidateStreams(List<PlaylistItem> streams, Dictionary<string, PlaylistItem> playlistItems)
        {
            _logging.Print("\nValidating streams. This might disconnect all active streams.");
            Validate(streams);

            if (!streams.Any(e => !e.IsValid))
            {
                return;
            }

            _logging.Info("\nBroken streams found, trying to find fallback channels");

            foreach (var invalidStream in streams.ToList().Where(e => !e.IsValid))
            {
                var fallbackStream = GetFallbackStream(invalidStream.TvgName, playlistItems);

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
            if (!_settingsProvider.Validation.ShowInvalid){
                streams = streams.Where(e => e.IsValid).ToList();
            }
        }

        public bool ValidateStreamByUrl(string url)
        {
            return CheckAvailability(url);
        }

        public bool ValidateSingleStream(PlaylistItem stream)
        {
            return CheckAvailability(stream.Url);
        }

        private void Validate(List<PlaylistItem> streams)
        {
            var streamCount = streams.Count();
            var i = 0;

            foreach (var stream in streams.ToList())
            {
                if (!CheckAvailability(stream.Url))
                {
                    stream.IsValid = false;
                }

                i++;

                _logging.Progress("- Progress", i, streamCount);
            }
        }

        private bool CheckAvailability(string url)
        {
            try
            {
                return _httpClientWrapper.Validate(url, _settingsProvider.Validation.ContentTypes, _settingsProvider.Validation.MinimumContentLength).Result;
            }
            catch (Exception)
            {
                return false;
            }
        }



        private PlaylistItem GetFallbackStream(string tvgName, Dictionary<string, PlaylistItem> playlistItems)
        {
            var specificFallback = GetSpecificFallback(tvgName, playlistItems);
            if (specificFallback != null)
            {
                return specificFallback;
            }

            var fallbackStream = GetDefaultFallback(tvgName, playlistItems);
            if (fallbackStream != null)
            {
                return fallbackStream;
            }

            return null;
        }

        public PlaylistItem GetSourceFallback(string id, Dictionary<string, PlaylistItem> channels){
            var fallbacks = GetFallbacks(id);

            if(fallbacks == null){
                return null;
            }

            foreach(var fallback in fallbacks.Value){
                var fallbackTvgName = id.Replace(fallbacks.Key, fallback);

                if (!channels.ContainsKey(fallbackTvgName))
                {
                    continue;
                }

                return channels[fallbackTvgName];
            }

            return null;
        }

        public UserSettingsValidationFallback GetFallbacks(string originalTvgName){
            if (_settingsProvider.Validation.DefaultFallbacks == null)
            {
                return null;
            }

            var tvgNames = _settingsProvider.Validation.DefaultFallbacks.FirstOrDefault(e => originalTvgName.Contains(e.Key));


            if (tvgNames == null || tvgNames.Value == null)
            {
                return null;
            }

            return tvgNames;
        }
        
        private PlaylistItem GetDefaultFallback(string originalTvgName, Dictionary<string, PlaylistItem> playlistItems)
        {

            var tvgNames = GetFallbacks(originalTvgName);

            if(tvgNames == null){
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
                var isValid = ValidateStreamByUrl(fallback.Url);
                if (!isValid)
                {
                    continue;
                }

                var channelSetting = _settingsProvider.Streams.FirstOrDefault(e => e.ChannelId == originalTvgName);

                if(channelSetting == null){
                    return null;
                }
                
                return _playlistItemMapper.Map(fallback, channelSetting);
            };

            return null;
        }

        private PlaylistItem GetSpecificFallback(string tvgName, Dictionary<string, PlaylistItem> playlistItems)
        {
            var channelSetting = _settingsProvider.Streams.FirstOrDefault(e => e.ChannelId == tvgName);
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

                var isValid = ValidateStreamByUrl(fallbackChannel.Url);

                if (isValid)
                {
                    return _playlistItemMapper.Map(fallbackChannel, channelSetting);
                }

            };

            return null;
        }

    }
}