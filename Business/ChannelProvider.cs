using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Couchpotato.Models;
namespace Couchpotato.Business{
    public class ChannelProvider: IChannelProvider{
        private readonly ISettingsProvider settingsProvider;
        private readonly IFileHandler fileHandler;
        private readonly IStreamValidator streamValidator;

        public ChannelProvider(ISettingsProvider settingsProvider, IFileHandler fileHandler, IStreamValidator streamValidator) {
            this.settingsProvider = settingsProvider;
            this.fileHandler = fileHandler;
            this.streamValidator = streamValidator;
        }

        public ChannelResult GetChannels(string path, Settings settings){
            var result = new ChannelResult();
            var playlistFile = Load(path);
            var playlistItems = Parse(playlistFile);

            var streams = new List<Channel>();

            if(settings.Channels.Any()){
                var channels = GetSelectedChannels(playlistItems, settings);
                streams.AddRange(channels);
            }

            if(settings.Groups.Any()){
                var groups = GetSelectedGroups(playlistItems, settings);
                streams.AddRange(groups);
            }
            
            if(settings.ValidateStreams){
                Console.WriteLine("\nValidating streams. This might disconnect all active streams.");
                var invalidStreams = this.streamValidator.ValidateStreams(streams);

                if(invalidStreams != null && invalidStreams.Count > 0){
                    Console.WriteLine("\nBroken streams found, trying to find fallback channels");

                    foreach(var invalidStreamTvgName in invalidStreams){
                        var fallbackChannel = GetFallbackChannel(invalidStreamTvgName, playlistItems, settings);

                        if(fallbackChannel != null){
                            Console.WriteLine($"- Fallback found for {invalidStreamTvgName}, now using {fallbackChannel.TvgName}");
                            streams.Add(fallbackChannel);
                        }else{
                            Console.WriteLine($"- Sorry, no fallback found for {invalidStreamTvgName}");
                        }
                    }
                    
                }
            }

            result.Channels = streams;

            return result;
        }

        private Channel GetFallbackChannel(string tvgName, List<PlaylistItem> playlistItems, Settings settings){
            var specificFallback = GetChannelSpecificFallback(tvgName, playlistItems, settings);
            if(specificFallback != null){
                return specificFallback;
            }

            var defaultFallback = GetDefaultFallback(tvgName, playlistItems, settings);
            if(defaultFallback != null){
                return defaultFallback;
            }

            return null;            
        }

        private Channel GetDefaultFallback(string tvgName, List<PlaylistItem> playlistItems, Settings settings){
            
            if(settings.DefaultChannelFallbacks != null){
                return null;
            }

            var fallbackChannelTvgNames = settings.DefaultChannelFallbacks.FirstOrDefault(e => tvgName.Contains(e.Key));

            if(fallbackChannelTvgNames == null || fallbackChannelTvgNames.Value == null){
                return null;
            }

            foreach(var fallbackChannelTvgName in fallbackChannelTvgNames.Value){
                var fallbackTvgName = tvgName.Replace(fallbackChannelTvgNames.Key, fallbackChannelTvgName);
                var fallbackChannel = playlistItems.FirstOrDefault(e => e.TvgName == fallbackTvgName);

                if(fallbackChannel != null){
                    var isValid = this.streamValidator.ValidateStreamByUrl(fallbackChannel.Url);

                    if(!isValid){
                        continue;
                    }
                    
                    var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
                    return MapChannel(fallbackChannel, channelSetting, settings);
                }
            };

            return null;
        }

        private Channel GetChannelSpecificFallback(string tvgName, List<PlaylistItem> playlistItems, Settings settings){
            var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
            if(channelSetting != null && channelSetting.FallbackChannels != null){
               foreach(var fallbackChannelId in channelSetting.FallbackChannels){
                   var fallbackChannel = playlistItems.FirstOrDefault(e => e.TvgName == fallbackChannelId);

                   if(fallbackChannel != null){
                       var isValid = this.streamValidator.ValidateStreamByUrl(fallbackChannel.Url);

                        if(isValid){
                            return MapChannel(fallbackChannel, channelSetting, settings);  
                        }
                   }
               };
            }

            return null;
        }

        private Channel MapChannel(PlaylistItem playlistItem, SettingsChannel channelSetting, Settings settings){
            var channel = new Channel();
            channel.TvgName = playlistItem.TvgName;
            channel.TvgId = playlistItem.TvgId;
            channel.TvgLogo = playlistItem.TvgLogo;
            channel.Url = playlistItem.Url;

            if(!string.IsNullOrEmpty(channelSetting.CustomGroupName) || !string.IsNullOrEmpty(settings.DefaultChannelGroup)){
                channel.GroupTitle = channelSetting.CustomGroupName ?? settings.DefaultChannelGroup;
            }else{
                channel.GroupTitle = playlistItem.GroupTitle;
            }

            if(!string.IsNullOrEmpty(channelSetting.FriendlyName)){
                channel.FriendlyName = channelSetting.FriendlyName;
            }

            channel.Order = settings.Channels.IndexOf(channelSetting);

            return channel;
        }

        private List<Channel> GetSelectedChannels(List<PlaylistItem> channels, Settings settings){
            
            var streams = new List<Channel>();

            foreach(var channel in settings.Channels){
                var channelSetting = channels.FirstOrDefault(e => e.TvgName == channel.ChannelId);
                if(channelSetting != null){
                    var channelItem = MapChannel(channelSetting, channel, settings);                    
                    streams.Add(channelItem);
                }else{
                    Console.Write($"\nCan't find channel { channel.ChannelId }");
                }
            }

            return streams;
        }


        private List<Channel> GetSelectedGroups(List<PlaylistItem> channels, Settings settings){
            var streams = new List<Channel>();

            foreach(var item in channels){
                var groupSettings = settings.Groups.FirstOrDefault(e => e.GroupId == item.GroupTitle);
                
                if(groupSettings != null){
                    var group = new Channel();
                    group.TvgName = item.TvgName;
                    group.TvgId = item.TvgId;
                    group.TvgLogo = item.TvgLogo;
                    group.Url = item.Url;
                    
                    if(!string.IsNullOrEmpty(groupSettings.FriendlyName)){
                        group.GroupTitle = groupSettings.FriendlyName;
                    }

                    if(!string.IsNullOrEmpty(groupSettings.FriendlyName)){
                        group.FriendlyName =  groupSettings.FriendlyName;
                    }

                    group.Order = settings.Channels.Count() + settings.Groups.IndexOf(groupSettings);

                    if(groupSettings.Exclude != null && groupSettings.Exclude.Any(e => e == item.TvgName)){
                        continue;
                    }

                    streams.Add(group);
                }
            }

            return streams;
        }

        private string[] Load(string path){
            Console.WriteLine("Loading channel list");
            var result = this.fileHandler.GetSource(path);

            if(result == null){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- Couldn't download file {path}");
                Console.ForegroundColor = ConsoleColor.White;
                return new string[]{};
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

        public void Save(string path, List<Channel> channels){
            Console.WriteLine($"Writing M3U-file to {path}"); 

            using (System.IO.StreamWriter writeFile =  new System.IO.StreamWriter(path, false, new UTF8Encoding(true))) {
                writeFile.WriteLine("#EXTM3U");

                foreach (Channel channel in channels.OrderBy(e => e.Order))
                {
                    var name = channel.FriendlyName ?? channel.TvgName;
                    writeFile.WriteLine($"#EXTINF:-1 tvg-id=\"{channel.TvgId}\" tvg-name=\"{channel.TvgName}\" tvg-logo=\"{channel.TvgLogo}\" group-title=\"{channel.GroupTitle}\",{name}");
                    writeFile.WriteLine(channel.Url);
                }
            }
        }

        private List<PlaylistItem> Parse(string[] file)
        {
            var streams = new List<PlaylistItem>();
            var numberOfLines = file.Length;

            for (var i = 1; i < numberOfLines; i = i + 2)
            {
                var item = file[i];

                if(!item.StartsWith("#EXTINF:-1")){
                    continue;
                } 

                var playlistItem = new PlaylistItem();
                playlistItem.TvgName = GetValueForAttribute(item, "tvg-name");
                playlistItem.GroupTitle = GetValueForAttribute(item, "group-title");
                playlistItem.TvgId =  GetValueForAttribute(item, "tvg-id");
                playlistItem.TvgLogo =  GetValueForAttribute(item, "tvg-logo");
                playlistItem.Url =  file[i + 1];
                
                streams.Add(playlistItem);
                        
                Console.Write($"\rCrunching playlist data: {((decimal)i / (decimal)numberOfLines).ToString("0%")}");
            }

            return streams;
        }

        private string GetValueForAttribute(string item, string attributeName){
            var result = new Regex(attributeName + @"=\""([^""]*)\""", RegexOptions.Singleline).Match(item);
            
            if(result == null || result.Groups.Count < 1){
                return string.Empty;
            }
            
            return result.Groups[1].Value;
        }
    }
}