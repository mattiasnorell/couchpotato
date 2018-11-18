using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Couchpotato.Models;

public class ChannelProvider:ProviderBase, IChannelProvider{
    private readonly ISettingsProvider settingsProvider;

    public ChannelProvider(ISettingsProvider settingsProvider){
        this.settingsProvider = settingsProvider;
    }

    public List<Channel> GetChannels(string path, Settings settings){
        var channelFile = Load(path);
        var channelHashTabel = Parse(channelFile);

        if(settings.ValidateChannels){
            // TODO: Implement validation
        }

        return channelHashTabel;
    }

    private bool CheckChannelAvailability(string url){
        var request  = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        const int maxBytes = 1024;
        request.AddRange(0, maxBytes-1);

        try{
            using(WebResponse response = request.GetResponse()){
                request.Abort();
                return true;
            }
        }catch(Exception ex){
            return false;
        }

        return false;
    }

    private string[] Load(string path){
        if(path.StartsWith("http")){
            Console.WriteLine("Downloading channel list");

            var result = DownloadFile(path);
            var list  = new List<string>();

            if(result == null){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- Couldn't download file {path}");
                Console.ForegroundColor = ConsoleColor.White;
                return list.ToArray();
            }
            
            using (var sr = new StreamReader(result))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }

            return list.ToArray();
            
        }else{
            Console.WriteLine($"Loading local channel list from {path}");

            if(!File.Exists(path)){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- Couldn't find file {path}");
                Console.ForegroundColor = ConsoleColor.White;
                return new string[]{};
            }

            return File.ReadAllLines(path);
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

    private List<Channel> Parse(string[] file)
    {
        var streams = new List<Channel>();
        var numberOfLines = file.Length;
    //    var settingChannels = settings.Channels;
        
  //      var parseChannels = settings.Channels.Any();
//        var parseGroups = settings.Groups.Any();

        for (var i = 1; i < numberOfLines; i = i + 2)
        {
            var item = file[i];

            if(!item.StartsWith("#EXTINF:-1")){
                continue;
            } 

            var channel = new Channel();
            channel.TvgName = GetValueForAttribute(item, "tvg-name");
            channel.GroupTitle = GetValueForAttribute(item, "group-title");
            channel.TvgId =  GetValueForAttribute(item, "tvg-id");
            channel.TvgLogo =  GetValueForAttribute(item, "tvg-logo");
            channel.Url =  file[i + 1];
            
            streams.Add(channel);
                    
          /*   if(parseChannels){
                var channelSetting = settings.Channels.FirstOrDefault(e => e.ChannelId == tvgName);
                if(channelSetting != null){
                    var channel = new Channel();
                    channel.TvgName = tvgName;
                    channel.GroupTitle = channelSetting.CustomGroupName ?? settings.DefaultChannelGroup;
                    channel.FriendlyName = channelSetting.FriendlyName;
                    channel.TvgId =  GetValueForAttribute(item, "tvg-id");
                    channel.TvgLogo =  GetValueForAttribute(item, "tvg-logo");
                    channel.Url =  file[i + 1];
                    channel.Order = settings.Channels.IndexOf(channelSetting);
                    
                    if(CheckChannelAvailability(channel.Url)){
                        streams.Add(channel);
                    }
                }
            }

            if(parseGroups){

                var groupTitle = GetValueForAttribute(item, "group-title");
                var group = settings.Groups.FirstOrDefault(e => e.GroupId == groupTitle);
                
                if(group != null){
                    var groupItem = new Channel();
                    groupItem.TvgName = tvgName;
                    groupItem.GroupTitle = group.FriendlyName ?? groupTitle;
                    groupItem.FriendlyName =  groupItem.FriendlyName;
                    groupItem.TvgId =  GetValueForAttribute(item, "tvg-id");
                    groupItem.TvgLogo =  GetValueForAttribute(item, "tvg-logo");
                    groupItem.Url =  file[i + 1];
                    groupItem.Order = settings.Channels.Count() + settings.Groups.IndexOf(group);

                    if(group.Exclude != null && group.Exclude.Any(e => e == tvgName)){
                        continue;
                    }

                    streams.Add(groupItem);
                }
            }
*/
            Console.Write($"\rCrunching channel data: {((decimal)i / (decimal)numberOfLines).ToString("0%")}");
        }

        return streams;
    }
}