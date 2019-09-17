using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Validation;
using Couchpotato.Core.Playlist;
using Couchpotato.Business.Playlist.Models;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Business.IO;
using Couchpotato.Business.Settings;
using System.IO;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistProvider : IPlaylistProvider
    {
        private readonly ISettingsProvider _settingsProvider;
        private readonly IFileHandler _fileHandler;
        private readonly IStreamValidator _streamValidator;
        private readonly ILogging _logging;
        private readonly IPlaylistParser _playlistParser;

        public PlaylistProvider(
            ISettingsProvider settingsProvider,
            IFileHandler fileHandler,
            IStreamValidator streamValidator,
            ILogging logging, 
            IPlaylistParser playlistParser)
        {
            _settingsProvider = settingsProvider;
            _fileHandler = fileHandler;
            _streamValidator = streamValidator;
            _logging = logging;
            _playlistParser = playlistParser;
        }

        public List<PlaylistItem> GetPlaylist(string path, UserSettings settings)
        {
            var playlistFile = Load(path);
            var playlistParsed = _playlistParser.Parse(playlistFile);
            var playlistItems = new List<PlaylistItem>();

            if (settings.Channels.Any())
            {
                var items = GetSelectedChannels(playlistParsed, settings);
                playlistItems.AddRange(items);
            }

            if (settings.Groups.Any())
            {
                var groupItems = GetSelectedGroups(playlistParsed, settings);
                playlistItems.AddRange(groupItems);
            }

            if (settings.ValidateStreams)
            {
                ValidateStreams(playlistItems, playlistParsed, settings);
            }

            return playlistItems;
        }

        private void ValidateStreams(List<PlaylistItem> streams, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            _logging.Print("\nValidating streams. This might disconnect all active streams.");
            var invalidStreams = _streamValidator.ValidateStreams(streams);

            if (invalidStreams == null || invalidStreams.Count == 0)
            {
                return;
            }

            _logging.Info("\nBroken streams found, trying to find fallback channels");

            foreach (var invalidStreamTvgName in invalidStreams)
            {
                var fallbackStream = GetFallbackStream(invalidStreamTvgName, playlistItems, settings);

                if (fallbackStream != null)
                {
                    _logging.Info($"- Fallback found for {invalidStreamTvgName}, now using {fallbackStream.TvgName}");
                    streams.Add(fallbackStream);
                }
                else
                {
                    _logging.Warn($"- Sorry, no fallback found for {invalidStreamTvgName}");
                }
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

            if (settings.DefaultChannelFallbacks == null)
            {
                return null;
            }

            var tvgNames = settings.DefaultChannelFallbacks.FirstOrDefault(e => originalTvgName.Contains(e.Key));

            if (tvgNames == null || tvgNames.Value == null)
            {
                return null;
            }

            foreach (var tvgName in tvgNames.Value)
            {
                var fallbackTvgName = tvgName.Replace(tvgNames.Key, tvgName);

                if (!playlistItems.ContainsKey(fallbackTvgName))
                {
                    continue;
                }

                var fallback = playlistItems[fallbackTvgName];
                var isValid = _streamValidator.ValidateStreamByUrl(fallback.Url);
                if (!isValid)
                {
                    continue;
                }

                var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
                return Map(fallback, channelSetting, settings);
            };

            return null;
        }

        private PlaylistItem GetSpecificFallback(string tvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
            if (channelSetting == null || channelSetting.FallbackChannels == null)
            {
                return null;
            }

            foreach (var fallbackChannelId in channelSetting.FallbackChannels)
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

                var isValid = _streamValidator.ValidateStreamByUrl(fallbackChannel.Url);

                if (isValid)
                {
                    return Map(fallbackChannel, channelSetting, settings);
                }

            };

            return null;
        }

        private PlaylistItem Map(PlaylistItem playlistItem, UserSettingsChannel channelSetting, UserSettings settings)
        {
            var channel = new PlaylistItem()
            {
                TvgName = playlistItem.TvgName,
                TvgId = channelSetting.EpgId ?? playlistItem.TvgId,
                TvgLogo = playlistItem.TvgLogo,
                Url = playlistItem.Url
            };

            if (!string.IsNullOrEmpty(channelSetting.CustomGroupName) || !string.IsNullOrEmpty(settings.DefaultChannelGroup))
            {
                channel.GroupTitle = channelSetting.CustomGroupName ?? settings.DefaultChannelGroup;
            }
            else
            {
                channel.GroupTitle = playlistItem.GroupTitle;
            }

            if (!string.IsNullOrEmpty(channelSetting.FriendlyName))
            {
                channel.FriendlyName = channelSetting.FriendlyName;
            }

            channel.Order = settings.Channels.IndexOf(channelSetting);

            return channel;
        }

        private List<PlaylistItem> GetSelectedChannels(Dictionary<string, PlaylistItem> channels, UserSettings settings)
        {
            var streams = new List<PlaylistItem>();
            var brokenStreams = new List<String>();

            foreach (var channel in settings.Channels)
            {
                if (channels.ContainsKey(channel.ChannelId))
                {
                    var channelSetting = channels[channel.ChannelId];
                    var channelItem = Map(channelSetting, channel, settings);
                    streams.Add(channelItem);
                }
                else
                {
                    brokenStreams.Add(channel.ChannelId);
                }
            }

            if (brokenStreams.Any())
            {
                _logging.Warn($"\nCan't find some channels:");

                foreach (var brokenStream in brokenStreams)
                {
                    _logging.Warn($"- {brokenStream }");
                }
            }

            return streams;
        }


        private List<PlaylistItem> GetSelectedGroups(Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            var streams = new List<PlaylistItem>();

            foreach (var group in settings.Groups)
            {
                var groupItems = playlistItems?.Values.Where(e => e.GroupTitle == group.GroupId).ToList();

                if (groupItems == null || !groupItems.Any())
                {
                    continue;
                }

                foreach (var groupItem in groupItems)
                {
                    if (group.Exclude != null && group.Exclude.Any(e => e == groupItem.TvgName))
                    {
                        continue;
                    }

                    var stream = new PlaylistItem()
                    {
                        TvgName = groupItem.TvgName,
                        TvgId = groupItem.TvgId,
                        TvgLogo = groupItem.TvgLogo,
                        Url = groupItem.Url,
                        GroupTitle = group.FriendlyName ?? group.GroupId,
                        Order = settings.Channels.Count() + groupItems.IndexOf(groupItem)
                    };

                    streams.Add(stream);
                }
            }

            return streams;
        }

        private string[] Load(string path)
        {
            _logging.Print("Loading channel list");
            var result = _fileHandler.GetSource(path);

            if (result == null)
            {
                _logging.Error($"- Couldn't download file {path}");
                return new string[] { };
            }

            using (var sr = new StreamReader(result))
            {
                string line;
                var list = new List<string>();

                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(line);
                }

                return list.ToArray();
            }
        }

        public string Save(string path, string fileName, List<PlaylistItem> channels)
        {
            _logging.Print($"Writing M3U-file to {path}");

            var content = new List<string>();
            content.Add("#EXTM3U");

            foreach (var channel in channels.OrderBy(e => e.Order))
            {
                var name = channel.FriendlyName ?? channel.TvgName;
                content.Add($"#EXTINF:-1 tvg-id=\"{channel.TvgId}\" tvg-name=\"{channel.TvgName}\" tvg-logo=\"{channel.TvgLogo}\" group-title=\"{channel.GroupTitle}\",{name}");
                content.Add(channel.Url);
            }

            return _fileHandler.WriteTextFile(path, fileName, content.ToArray());
        }
    }
}