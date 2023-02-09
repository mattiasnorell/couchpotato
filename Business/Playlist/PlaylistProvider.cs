using System;
using System.Collections.Generic;
using System.Linq;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Validation;
using Couchpotato.Core.Playlist;
using Couchpotato.Business.IO;
using Couchpotato.Business.Settings;
using System.IO;
using System.Text;
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
                if (channels.TryGetValue(channel.ChannelId, out var value))
                {
                    var channelItem = _playlistItemMapper.Map(value, channel);
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
                    _logging.Warn($"- {brokenStream}");
                }
            }

            result.Items = streams;
            return result;
        }


        private IEnumerable<PlaylistItem> GetSelectedGroups(Dictionary<string, PlaylistItem> playlistItems)
        {
            return _settingsProvider.Groups.SelectMany(group =>
            {
                var groupItems = playlistItems?.Values.Where(e => e.GroupTitle == group.GroupId).ToList();

                if (groupItems == null)
                {
                    return Enumerable.Empty<PlaylistItem>();
                }

                return groupItems
                    .Where(groupItem => group.Exclude == null || !group.Exclude.Contains(groupItem.TvgName))
                    .Select((groupItem, index) => new PlaylistItem
                    {
                        TvgName = groupItem.TvgName,
                        TvgId = groupItem.TvgId,
                        TvgLogo = groupItem.TvgLogo,
                        Url = groupItem.Url,
                        GroupTitle = group.FriendlyName ?? group.GroupId,
                        Order = _settingsProvider.Streams.Count + groupItems.IndexOf(groupItem)
                    });
            });
        }

        private string[] Load(string path)
        {
            var cacheResult = _cacheProvider.Get(path, _settingsProvider.PlaylistCacheDuration);
            if (cacheResult != null)
            {
                _logging.Print("- Loaded playlist from cache");
                return StreamToArray(cacheResult);
            }

            var downloadResult = _fileHandler.GetSource(path);
            if (downloadResult != null)
            {
                _logging.Print($"- Download playlist from {path}");
                _cacheProvider.Set(path, downloadResult);
                return StreamToArray(downloadResult);
            }


            _logging.Error($"- Couldn't download file {path}");
            return new string[] { };
        }

        private static string[] StreamToArray(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            using var sr = new StreamReader(stream);
            var content = sr.ReadToEnd();
            return content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        }

        public string Save(string path, string fileName, List<PlaylistItem> channels)
        {
            _logging.Print($"Writing M3U-file to {path}/{fileName}");

            var builder = new StringBuilder();
            builder.AppendLine("#EXTM3U");

            foreach (var channel in channels.OrderBy(e => e.Order))
            {
                var name = channel.FriendlyName ?? channel.TvgName;
                builder.AppendLine(
                    $"#EXTINF:-1 tvg-id=\"{channel.TvgId}\" tvg-name=\"{channel.TvgName}\" tvg-logo=\"{channel.TvgLogo}\" group-title=\"{channel.GroupTitle}\",{name}");
                builder.AppendLine(channel.Url);
            }

            return _fileHandler.WriteTextFile(path, fileName, builder.ToString());
        }
    }
}