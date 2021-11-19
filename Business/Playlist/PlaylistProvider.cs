using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Validation;
using Couchpotato.Core.Playlist;
using Couchpotato.Business.IO;
using Couchpotato.Business.Settings;
using System.IO;
using Couchpotato.Business.Cache;

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
        private readonly ICacheProvider _cacheProvider;

        public PlaylistProvider(
            IFileHandler fileHandler,
            IStreamValidator streamValidator,
            ILogging logging,
            IPlaylistParser playlistParser,
            IPlaylistItemMapper playlistItemMapper,
            ISettingsProvider settingsProvider,
            ICacheProvider cacheProvider)
        {
            _fileHandler = fileHandler;
            _streamValidator = streamValidator;
            _logging = logging;
            _playlistParser = playlistParser;
            _playlistItemMapper = playlistItemMapper;
            _settingsProvider = settingsProvider;
            _cacheProvider = cacheProvider;

        }

        public PlaylistResult GetPlaylist()
        {
            var result = new PlaylistResult();
            var playlistFile = Load(_settingsProvider.Source);
            var playlistParsed = _playlistParser.Parse(playlistFile);
            var playlistSingleItems = new List<PlaylistItem>();
            var playlistGroupItems = new List<PlaylistItem>();

            if (_settingsProvider.Streams.Any())
            {
                var singleItems = GetSelectedChannels(playlistParsed);
                playlistSingleItems.AddRange(singleItems.Items);
                result.Missing.AddRange(singleItems.Missing);
            }

            if (_settingsProvider.Groups.Any())
            {
                var groupItems = GetSelectedGroups(playlistParsed);
                playlistGroupItems.AddRange(groupItems);
            }

            if (playlistSingleItems.Count > 0 && _settingsProvider.Validation.SingleEnabled)
            {
                _streamValidator.ValidateStreams(playlistSingleItems, playlistParsed);
            }

            if (playlistGroupItems.Count > 0 && _settingsProvider.Validation.GroupEnabled)
            {
                _streamValidator.ValidateStreams(playlistGroupItems, playlistParsed);
            }

            result.Items.AddRange(playlistSingleItems);
            result.Items.AddRange(playlistGroupItems);

            return result;
        }

        private PlaylistResult GetSelectedChannels(Dictionary<string, PlaylistItem> channels)
        {
            var streams = new List<PlaylistItem>();
            var brokenStreams = new List<string>();
            var result = new PlaylistResult();

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
                    if (_settingsProvider.Validation.SingleEnabled)
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
                    result.Missing.Add(brokenStream);
                    _logging.Warn($"- { brokenStream }");
                }
            }

            result.Items = streams;
            return result;
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
            _logging.Print("Loading playlist");
            var result = _cacheProvider.Get(path, this._settingsProvider.PlaylistCacheDuration);

            if(result == null)
            {
                result = _fileHandler.GetSource(path);
            }
            else
            {
                _logging.Print("Loaded playlist from cache");
            }

            if (result == null)
            {
                _logging.Error($"- Couldn't download file {path}");
                return new string[] { };
            }

            _cacheProvider.Set(path, result);

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