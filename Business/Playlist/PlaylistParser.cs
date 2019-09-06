using System.Collections.Generic;
using System.Text.RegularExpressions;
using Couchpotato.Business.Logging;
using Couchpotato.Business.Playlist.Models;

namespace Couchpotato.Business.Playlist
{
    public class PlaylistParser : IPlaylistParser
    {
        private readonly ILogging logging;

        public PlaylistParser(
            ILogging logging
        )
        {
            this.logging = logging;
        }

       public Dictionary<string, PlaylistItem> Parse(string[] file)
        {
            var streams = new Dictionary<string, PlaylistItem>();
            var numberOfLines = file.Length;

            for (var i = 1; i < numberOfLines; i = i + 2)
            {
                var item = file[i];

                if (!item.StartsWith("#EXTINF:-1"))
                {
                    continue;
                }
                
                var playlistItem = new PlaylistItem();
                var tvgName = GetValueForAttribute(item, "tvg-name");
                
                playlistItem.TvgName = tvgName;
                playlistItem.GroupTitle = GetValueForAttribute(item, "group-title");
                playlistItem.TvgId = GetValueForAttribute(item, "tvg-id");
                playlistItem.TvgLogo = GetValueForAttribute(item, "tvg-logo");
                playlistItem.Url = file[i + 1];

                if(!streams.ContainsKey(tvgName)){
                    streams.Add(tvgName, playlistItem);
                }
                
                this.logging.PrintSameLine($"Crunching playlist data: {((decimal)i / (decimal)numberOfLines).ToString("0%")}");
            }

            return streams;
        }

        private string GetValueForAttribute(string item, string attributeName)
        {
            var result = new Regex(attributeName + @"=\""([^""]*)\""", RegexOptions.Singleline).Match(item);

            if (result == null || result.Groups.Count < 1)
            {
                return string.Empty;
            }

            return result.Groups[1].Value;
        }
    }
}