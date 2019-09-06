using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Validation;
using CouchpotatoShared.Channel;
using Couchpotato.Business.Playlist.Models;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Business.IO;
using Couchpotato.Business.Settings;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistProvider : IPlaylistProvider
    {
        private readonly ISettingsProvider settingsProvider;
        private readonly IFileHandler fileHandler;
        private readonly IStreamValidator streamValidator;
        private readonly ILogging logging;
        private readonly IPlaylistParser playlistParser;

        public PlaylistProvider(ISettingsProvider settingsProvider, IFileHandler fileHandler, IStreamValidator streamValidator, ILogging logging, IPlaylistParser playlistParser)
        {
            this.settingsProvider = settingsProvider;
            this.fileHandler = fileHandler;
            this.streamValidator = streamValidator;
            this.logging = logging;
            this.playlistParser = playlistParser;
        }

        public ChannelResult GetPlaylist(string path, UserSettings settings)
        {
            var result = new ChannelResult();
            var playlistFile = Load(path);
            var playlistItems = playlistParser.Parse(playlistFile);

            var streams = new List<Channel>();

            if (settings.Channels.Any())
            {
                var items = GetSelectedChannels(playlistItems, settings);
                streams.AddRange(items);
            }

            if (settings.Groups.Any())
            {
                var groupItems = GetSelectedGroups(playlistItems, settings);
                streams.AddRange(groupItems);
            }

            if (settings.ValidateStreams)
            {
                ValidateStreams(streams, playlistItems, settings);
            }

            result.Channels = streams;

            return result;
        }

        private void ValidateStreams(List<Channel> streams, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            this.logging.Print("\nValidating streams. This might disconnect all active streams.");
            var invalidStreams = this.streamValidator.ValidateStreams(streams);

            if (invalidStreams != null && invalidStreams.Count > 0)
            {
                this.logging.Info("\nBroken streams found, trying to find fallback channels");

                foreach (var invalidStreamTvgName in invalidStreams)
                {
                    var fallbackStream = GetFallbackStream(invalidStreamTvgName, playlistItems, settings);

                    if (fallbackStream != null)
                    {
                        this.logging.Info($"- Fallback found for {invalidStreamTvgName}, now using {fallbackStream.TvgName}");
                        streams.Add(fallbackStream);
                    }
                    else
                    {
                        this.logging.Warn($"- Sorry, no fallback found for {invalidStreamTvgName}");
                    }
                }
            }
        }

        private Channel GetFallbackStream(string tvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
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

        private Channel GetDefaultFallback(string originalTvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
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
                var fallback = playlistItems[fallbackTvgName];

                if (fallback == null)
                {
                    continue;
                }

                var isValid = this.streamValidator.ValidateStreamByUrl(fallback.Url);
                if (!isValid)
                {
                    continue;
                }

                var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
                return Map(fallback, channelSetting, settings);
            };

            return null;
        }

        private Channel GetSpecificFallback(string tvgName, Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
            if (channelSetting != null && channelSetting.FallbackChannels != null)
            {
                foreach (var fallbackChannelId in channelSetting.FallbackChannels)
                {
                    var fallbackChannel = playlistItems[fallbackChannelId];

                    if (fallbackChannel == null)
                    {
                        continue;
                    }

                    var isValid = this.streamValidator.ValidateStreamByUrl(fallbackChannel.Url);

                    if (isValid)
                    {
                        return Map(fallbackChannel, channelSetting, settings);
                    }

                };
            }

            return null;
        }

        private Channel Map(PlaylistItem playlistItem, UserSettingsChannel channelSetting, UserSettings settings)
        {
            var channel = new Channel();
            channel.TvgName = playlistItem.TvgName;
            channel.TvgId = channelSetting.EpgId ?? playlistItem.TvgId;
            channel.TvgLogo = playlistItem.TvgLogo;
            channel.Url = playlistItem.Url;

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

        private List<Channel> GetSelectedChannels(Dictionary<string, PlaylistItem> channels, UserSettings settings)
        {
            var streams = new List<Channel>();
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
                this.logging.Warn($"\nCan't find some channels:");

                foreach (var brokenStream in brokenStreams)
                {
                    this.logging.Warn($"- {brokenStream }");
                }
            }

            return streams;
        }


        private List<Channel> GetSelectedGroups(Dictionary<string, PlaylistItem> playlistItems, UserSettings settings)
        {
            var streams = new List<Channel>();

            foreach (var group in settings.Groups)
            {
                var groupItems = playlistItems?.Values.Where(e => e.GroupTitle == group.GroupId).ToList();

                if (groupItems == null || !groupItems.Any())
                {
                    continue;
                }

                foreach (var groupItem in groupItems)
                {
                    var stream = new Channel();
                    stream.TvgName = groupItem.TvgName;
                    stream.TvgId = groupItem.TvgId;
                    stream.TvgLogo = groupItem.TvgLogo;
                    stream.Url = groupItem.Url;
                    stream.GroupTitle = group.FriendlyName ?? group.GroupId;
                    stream.Order = settings.Channels.Count() + groupItems.IndexOf(groupItem);

                    if (group.Exclude != null && group.Exclude.Any(e => e == groupItem.TvgName))
                    {
                        continue;
                    }

                    streams.Add(stream);
                }
            }

            return streams;
        }

        private string[] Load(string path)
        {
            this.logging.Print("Loading channel list");
            var result = this.fileHandler.GetSource(path);

            if (result == null)
            {
                this.logging.Error($"- Couldn't download file {path}");
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

        public void Save(string path, List<Channel> channels)
        {
            this.logging.Print($"Writing M3U-file to {path}");

            using (System.IO.StreamWriter writeFile = new System.IO.StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writeFile.WriteLine("#EXTM3U");

                foreach (Channel channel in channels.OrderBy(e => e.Order))
                {
                    var name = channel.FriendlyName ?? channel.TvgName;
                    writeFile.WriteLine($"#EXTINF:-1 tvg-id=\"{channel.TvgId}\" tvg-name=\"{channel.TvgName}\" tvg-logo=\"{channel.TvgLogo}\" group-title=\"{channel.GroupTitle}\",{name}");
                    writeFile.WriteLine(channel.Url);
                }
            }
        }
    }
}