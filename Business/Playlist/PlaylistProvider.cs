using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Validation;
using Couchpotato.Core.Playlist;
using Couchpotato.Business.IO;
using Couchpotato.Business.Settings;
using System.IO;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistProvider : IPlaylistProvider
    {
        private readonly IFileHandler _fileHandler;
        private readonly IStreamValidator _streamValidator;
        private readonly ILogging _logging;
        private readonly IPlaylistParser _playlistParser;
        private readonly IPlaylistItemMapper _playlistItemMapper;
        private readonly ISettingsProvider _settingsProvider;

        public PlaylistProvider(
            IFileHandler fileHandler,
            IStreamValidator streamValidator,
            ILogging logging,
            IPlaylistParser playlistParser,
            IPlaylistItemMapper playlistItemMapper,
            ISettingsProvider settingsProvider)
        {
            _fileHandler = fileHandler;
            _streamValidator = streamValidator;
            _logging = logging;
            _playlistParser = playlistParser;
            _playlistItemMapper = playlistItemMapper;
            _settingsProvider = settingsProvider;
        }

        public List<PlaylistItem> GetPlaylist()
        {
            var playlistFile = Load(_settingsProvider.Source);
            var playlistParsed = _playlistParser.Parse(playlistFile);
            var playlistItems = new List<PlaylistItem>();

            if (_settingsProvider.Streams.Any())
            {
                var items = GetSelectedChannels(playlistParsed);
                playlistItems.AddRange(items);
            }

            if (_settingsProvider.Groups.Any())
            {
                var groupItems = GetSelectedGroups(playlistParsed);
                playlistItems.AddRange(groupItems);
            }

            if (playlistItems.Count > 0 && _settingsProvider.Validation.Enabled)
            {
                _streamValidator.ValidateStreams(playlistItems, playlistParsed);
            }

            return playlistItems;
        }

        private List<PlaylistItem> GetSelectedChannels(Dictionary<string, PlaylistItem> channels)
        {
            var streams = new List<PlaylistItem>();
            var brokenStreams = new List<String>();

            foreach (var channel in _settingsProvider.Streams)
            {
                if (channels.ContainsKey(channel.ChannelId))
                {
                    var channelSetting = channels[channel.ChannelId];
                    var channelItem = _playlistItemMapper.Map(channelSetting, channel);
                    streams.Add(channelItem);
                }
                else
                {
                    if (_settingsProvider.Validation.Enabled)
                    {
                        var fallbackStream = _streamValidator.GetSourceFallback(channel.ChannelId, channels);

                        if (fallbackStream == null)
                        {
                            brokenStreams.Add(channel.ChannelId);
                            continue;
                        }

                        var fallbackChannelItem = _playlistItemMapper.Map(fallbackStream, channel);
                        streams.Add(fallbackChannelItem);
                        _logging.Info($"Could not find {channel.ChannelId}, using {fallbackChannelItem.TvgName}");
                    }
                    else
                    {
                        brokenStreams.Add(channel.ChannelId);
                    }
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


        private List<PlaylistItem> GetSelectedGroups(Dictionary<string, PlaylistItem> playlistItems)
        {
            var streams = new List<PlaylistItem>();

            foreach (var group in _settingsProvider.Groups)
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
                        Order = _settingsProvider.Streams.Count() + groupItems.IndexOf(groupItem)
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
            _logging.Print($"Writing M3U-file to {path}/{fileName}");

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