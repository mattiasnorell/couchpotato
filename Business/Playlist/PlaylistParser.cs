using System.Collections.Generic;
using System.Text.RegularExpressions;
using Couchpotato.Business.Logging;
using Couchpotato.Core.Playlist;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistParser : IPlaylistParser
    {
        private readonly ILogging _logging;

        public PlaylistParser(
            ILogging logging
        )
        {
            _logging = logging;
        }

        public Dictionary<string, PlaylistItem> Parse(string[] file)
        {
            var streams = new Dictionary<string, PlaylistItem>();
            var numberOfLines = file.Length;
            _logging.Print($"Crunching playlist data");

            for (var i = 1; i < numberOfLines; i = i + 2)
            {
                var item = file[i];
                var streamUrl = file[i + 1];

                if (!item.StartsWith("#EXTINF:-1") && !streamUrl.StartsWith("http"))
                {
                    i++;
                    continue;
                }

                if (!item.StartsWith("#EXTINF:-1"))
                {
                    continue;
                }

                var tvgName = GetValueForAttribute(item, "tvg-name");
                if (streams.ContainsKey(tvgName)) continue;

                var playlistItem = new PlaylistItem()
                {
                    TvgName = tvgName,
                    GroupTitle = GetValueForAttribute(item, "group-title"),
                    TvgId = GetValueForAttribute(item, "tvg-id"),
                    TvgLogo = GetValueForAttribute(item, "tvg-logo"),
                    Url = streamUrl
                };

                streams.Add(tvgName, playlistItem);
                
                   _logging.Progress($"Crunching playlist data", i, numberOfLines - 2);
            }

            return streams;
        }

        private static string GetValueForAttribute(string item, string attributeName)
        {
            var result = new Regex(attributeName + @"=\""([^""]*)\""", RegexOptions.Singleline).Match(item);

            return result.Groups.Count < 1 ? string.Empty : result.Groups[1].Value;
        }
    }
}