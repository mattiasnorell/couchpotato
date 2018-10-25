using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using Couchpotato.Models;
using System.Net;
using System.Reflection;
using System.IO.Compression;

namespace Couchpotato
{

    class Program
    {
        static void Main(string[] args)
        {
            var settings = LoadSettings(args[0]);
            var channelFile = GetM3uSource(settings.M3uPath);
            var channels = ParseChannelList(channelFile, settings);
            var epgFile = LoadEpgFiles(settings.EpgPath);
            var filteredEpgFile = FilterEpgList(epgFile, settings);
            var outputPath = settings.OutputPath ?? "./";

            if(!Directory.Exists(outputPath)){
                Directory.CreateDirectory(outputPath); 
            }

            var outputM3uPath = Path.Combine(outputPath, "channels.m3u");
            WriteM3uFile(outputM3uPath, channels);

            var outputEpgPath = Path.Combine(outputPath, "epg.xml");
            WriteEpgFile(outputEpgPath, filteredEpgFile);
            

            if(settings.Compress){
                Compress(outputM3uPath);
                Compress(outputEpgPath);
            }

            Console.WriteLine("Done!");
        }

        static EpgList LoadEpgFiles(string[] paths){
            Console.WriteLine($"\nLoading EPG-files:");

            var epgList = new EpgList();
            epgList.GeneratorInfoName = "";
            epgList.GeneratorInfoUrl = "";
            epgList.Channels = new List<EpgChannel>();
            epgList.Programs = new List<EpgProgram>();

            foreach(var path in paths){
                var epgFile = ParseEpgList(path);

                epgList.Channels.AddRange(epgFile.Channels);
                epgList.Programs.AddRange(epgFile.Programs);
            }    

            return epgList;
        }

        static EpgList ParseEpgList(string path){
            XmlRootAttribute xRoot = new XmlRootAttribute();
            xRoot.ElementName = "tv";
            xRoot.IsNullable = true;
            XmlSerializer serializer = new XmlSerializer(typeof(EpgList), xRoot);

            using(var stream = GetEpgSource(path)){
                return (EpgList)serializer.Deserialize(stream);          
            };
        }

        static EpgList FilterEpgList(EpgList input, Settings settings){
            var epgFile = new EpgList();
            epgFile.GeneratorInfoName = "Couchpotato";
            epgFile.Channels = new List<EpgChannel>();
            epgFile.Programs = new List<EpgProgram>();

            var channelCount = settings.Channels.Count;
            var i = 0;

            foreach(var settingsChannel in settings.Channels){
                i = i + 1;
                var epgId = settingsChannel.EpgId ?? settingsChannel.ChannelId;
                var channel = input.Channels.FirstOrDefault(e => e.Id == epgId);
                Console.Write("\rFiltering EPG-files: " + ((decimal)i / (decimal)channelCount).ToString("0%"));

                if(channel == null){
                    continue;
                }

                var epgChannel = new EpgChannel();
                epgChannel.Id = channel.Id;
                epgChannel.DisplayName = settingsChannel.FriendlyName ?? channel.DisplayName;
                epgChannel.Url = channel.Url;

                epgFile.Channels.Add(epgChannel);

                foreach(var program in input.Programs.Where(e=> e.Channel == channel.Id)){
                    var epgProgram = new EpgProgram();
                    epgProgram.Channel = program.Channel;
                    epgProgram.Desc = program.Desc;
                    epgProgram.EpisodeNumber = program.EpisodeNumber;
                    epgProgram.Lang = program.Lang;
                    epgProgram.Start = program.Start;
                    epgProgram.Stop = program.Stop;
                    epgProgram.Title = program.Title;

                    epgFile.Programs.Add(epgProgram);
                }
            }

            return epgFile;
        }

        static Channel MapChannel(string tvgName, string item, string url, SettingsChannel channelSetting, Settings settings){
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

        static Channel MapGroup(string tvgName, string groupTitle, string item, string url, SettingsGroup settingsGroup, Settings settings){
            
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

        static List<Channel> ParseChannelList(string[] file, Settings settings){
            var streams = new List<Channel>();
            var numberOfLines = file.Length;
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

        static Settings LoadSettings(string path){
            Console.WriteLine("Loading settings from " + path);
            Settings settings;

            using (StreamReader responseReader = new StreamReader(path))
            {
                string response = responseReader.ReadToEnd();
                settings = JsonConvert.DeserializeObject<Settings>(response);
            }

            return settings;
        }

        static Stream DownloadFile(string path){
            using (var client = new WebClient())
            {
                return client.OpenRead(path);
            }
        }

        static Stream GetEpgSource(string path){
            if(path.StartsWith("http")){
                Console.WriteLine($"- Downloading EPG from {path}");
                return DownloadFile(path);
                
            }else{
                Console.WriteLine($"- Reading local EPG from {path}");
                return new FileStream(path, FileMode.Open);

            }
        }

        static string[] GetM3uSource(string path){
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

                return list.ToArray();
                
            }else{
                Console.WriteLine($"Loading local channel list from {path}");

                return File.ReadAllLines(path);
            }
        }

         static string GetValueForAttribute(string item, string attributeName){
            var result = new Regex(attributeName + @"=\""([^""]*)\""", RegexOptions.Singleline).Match(item);
            
            if(result == null || result.Groups.Count < 1){
                return string.Empty;
            }
            
            return result.Groups[1].Value;
        }

        static void WriteM3uFile(string path, List<Channel> channels){

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

        static void WriteEpgFile(string path, EpgList epgList){
            Console.WriteLine($"Writing EPG-file to {path}"); 

            System.Xml.Serialization.XmlSerializer writer =  new System.Xml.Serialization.XmlSerializer(typeof(EpgList));  
            System.IO.FileStream file = System.IO.File.Create(path);  

            writer.Serialize(file, epgList);  
            file.Close();  
        }

        static void Compress(string path){

            FileInfo sourceFile = new FileInfo(path);
            FileInfo targetFileName = new FileInfo($"{sourceFile.FullName}.gz");
                        
            using (FileStream sourceFileStream = sourceFile.OpenRead())
            {
                using (FileStream targetFileStream = targetFileName.Create())
                    {
                    using (GZipStream gzipStream = new GZipStream(targetFileStream, CompressionMode.Compress))
                    {
                        try
                        {
                            sourceFileStream.CopyTo(gzipStream);
                            Console.WriteLine($"Saving compressed file to {targetFileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Compression failed - {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
