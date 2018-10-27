using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Couchpotato.Models;

public class EpgProvider:ProviderBase, IEpgProvider{
    private readonly ICompression compression;

    public EpgProvider(ICompression compression){
        this.compression = compression;
    }

    public EpgList Load(string[] paths, Settings settings){
        Console.WriteLine($"\nLoading EPG-files:");

        var epgList = new EpgList();
        epgList.GeneratorInfoName = "";
        epgList.GeneratorInfoUrl = "";
        epgList.Channels = new List<EpgChannel>();
        epgList.Programs = new List<EpgProgram>();

        foreach(var path in paths){
            var epgFile = Parse(path);

            if(epgFile == null){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"- Couldn't download file {path}");
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            epgList.Channels.AddRange(epgFile.Channels);
            epgList.Programs.AddRange(epgFile.Programs);
        }    

        return Filter(epgList,settings);
    }

    private EpgList Parse(string path)
    {
        XmlRootAttribute xRoot = new XmlRootAttribute();
        xRoot.ElementName = "tv";
        xRoot.IsNullable = true;
        XmlSerializer serializer = new XmlSerializer(typeof(EpgList), xRoot);

        using(var stream = GetSource(path)){
            if(stream == null){
                return null;
            }

            return (EpgList)serializer.Deserialize(stream);          
        };
    }


     private Stream GetSource(string path){
        if(path.StartsWith("http")){
            Console.WriteLine($"- Downloading EPG from {path}");
            var file = DownloadFile(path);

            if(path.EndsWith(".gz")){
                Console.WriteLine($"- Decompressed file");
                return compression.Decompress(file);
            }

            return file;
            
        }else{
            Console.WriteLine($"- Reading local EPG from {path}");

            if(!File.Exists(path)){
                return null;
            }

            var file =  new FileStream(path, FileMode.Open);

            if(path.EndsWith(".gz")){
                Console.WriteLine($"- Decompressed file");
                return compression.Decompress(file);
            }

            return file;
           
        }
    }

    private EpgList Filter(EpgList input, Settings settings){
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

        Console.Write("\n");

        return epgFile;
    }

    public void Save(string path, EpgList epgList){
      Console.WriteLine($"Writing EPG-file to {path}"); 
        System.Xml.Serialization.XmlSerializer writer =  new System.Xml.Serialization.XmlSerializer(typeof(EpgList));  
        System.IO.FileStream file = System.IO.File.Create(path);  

        writer.Serialize(file, epgList);  
        file.Close(); 
    }
}