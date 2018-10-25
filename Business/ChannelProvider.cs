using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Couchpotato.Models;

public class ChannelProvider:ProviderBase, IChannelProvider{
    private readonly ISettingsProvider settingsProvider;

    public ChannelProvider(ISettingsProvider settingsProvider){
        this.settingsProvider = settingsProvider;
    }

    public List<Channel> Load(string path, Settings settings){
        if(path.StartsWith("http")){
            Console.WriteLine("Downloading channel list");

            var result = DownloadFile(path);
            var list  = new List<string>();
            using (var sr = new StreamReader(result))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }

            return Parse(list.ToArray(), settings);
            
        }else{
            Console.WriteLine($"Loading local channel list from {path}");

            return Parse(File.ReadAllLines(path), settings);
        }
    }

    public void Save(string path, List<Channel> channels){
        Console.WriteLine($"\nWriting M3U-file to {path}"); 

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

     private List<Channel> Parse(string[] file, Settings settings)
    {
        var streams = new List<Channel>();
        var numberOfLines = file.Length;
        var settingChannels = settings.Channels;
        
        var parseChannels = settings.Channels.Any();
        var parseGroups = settings.Groups.Any();

        for (var i = 1; i < numberOfLines; i = i + 2)
        {
            var item = file[i];

            if(!item.StartsWith("#EXTINF:-1")){
                continue;
            } 

            var tvgName = GetValueForAttribute(item, "tvg-name");

            if(parseChannels){
                var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
                if(channelSetting != null){
                    streams.Add(MapChannel(tvgName, item, file[i + 1], channelSetting, settings));
                }
            }

            if(parseGroups){
                var groupTitle = GetValueForAttribute(item, "group-title");
                var group = settings.Groups.FirstOrDefault(e => e.GroupId == groupTitle);
                if(group != null){
                    streams.Add(MapGroup(tvgName, groupTitle, item, file[i + 1], group, settings));
                }
            }

            Console.Write("\rCrunching channel data: " + ((decimal)i / (decimal)numberOfLines).ToString("0%"));
        }

        return streams;
    }

     private Channel MapChannel(string tvgName, string item, string url, SettingsChannel channelSetting, Settings settings){
        var channel = new Channel();
        channel.TvgName = tvgName;
        channel.GroupTitle = channelSetting.Group ?? settings.DefaultChannelGroup;
        channel.FriendlyName = channelSetting.FriendlyName;
        channel.TvgId =  GetValueForAttribute(item, "tvg-id");
        channel.TvgLogo =  GetValueForAttribute(item, "tvg-logo");
        channel.Url =  url;
        channel.Order = settings.Channels.IndexOf(channelSetting);

        return channel;
    }

    private Channel MapGroup(string tvgName, string groupTitle, string item, string url, SettingsGroup settingsGroup, Settings settings){
        
        var groupItem = new Channel();
        groupItem.TvgName = tvgName;
        groupItem.GroupTitle = settingsGroup.FriendlyName ?? groupTitle;
        groupItem.FriendlyName =  groupItem.FriendlyName;
        groupItem.TvgId =  GetValueForAttribute(item, "tvg-id");
        groupItem.TvgLogo =  GetValueForAttribute(item, "tvg-logo");
        groupItem.Url =  url;
        groupItem.Order = settings.Channels.Count() + settings.Groups.IndexOf(settingsGroup);

        return groupItem;
    }
}