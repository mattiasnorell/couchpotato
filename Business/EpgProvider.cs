using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Couchpotato.Models;

namespace Couchpotato.Business{
    public class EpgProvider: IEpgProvider{
        private readonly IFileHandler fileHandler;

        public EpgProvider(IFileHandler fileHandler){
            this.fileHandler = fileHandler;
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

            var filteredEpgList = Filter(epgList,settings);
            return filteredEpgList;
        }

        private EpgList Parse(string path)
        {
            XmlRootAttribute xRoot = new XmlRootAttribute();
            xRoot.ElementName = "tv";
            xRoot.IsNullable = true;
            XmlSerializer serializer = new XmlSerializer(typeof(EpgList), xRoot);

            using(var stream = this.fileHandler.GetSource(path)){
                if(stream == null){
                    return null;
                }

                return (EpgList)serializer.Deserialize(stream);          
            };
        }

        private EpgList Filter(EpgList input, Settings settings){
            var epgFile = new EpgList();
            epgFile.GeneratorInfoName = "Couchpotato";
            epgFile.Channels = new List<EpgChannel>();
            epgFile.Programs = new List<EpgProgram>();

            var channelCount = settings.Channels.Count;
            var i = 0;

            var missingChannels = new List<SettingsChannel>();

            foreach(var settingsChannel in settings.Channels){
                i = i + 1;
                var epgId = settingsChannel.EpgId ?? settingsChannel.ChannelId;
                var channel = input.Channels.FirstOrDefault(e => e.Id == epgId);
                Console.Write("\rFiltering EPG-files: " + ((decimal)i / (decimal)channelCount).ToString("0%"));

                if(channel == null){
                    missingChannels.Add(settingsChannel);
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
                    epgProgram.Start = string.IsNullOrEmpty(settingsChannel.EpgTimeshift) ? program.Start : AddTimeshift(program.Start, settingsChannel.EpgTimeshift);
                    epgProgram.Stop =  string.IsNullOrEmpty(settingsChannel.EpgTimeshift) ? program.Stop : AddTimeshift(program.Stop, settingsChannel.EpgTimeshift);
                    epgProgram.Title = program.Title;

                    epgFile.Programs.Add(epgProgram);
                }
            }

            Console.Write("\n");

            if(missingChannels.Any()){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Couldn't find EPG for:");
                
                foreach(var missingChannel in missingChannels){
                    Console.WriteLine($"- { missingChannel.FriendlyName}");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            return epgFile;
        }

        public void Save(string path, EpgList epgList){
            Console.WriteLine($"Writing EPG-file to {path}"); 
            System.Xml.Serialization.XmlSerializer writer =  new System.Xml.Serialization.XmlSerializer(typeof(EpgList));  
            System.IO.FileStream file = System.IO.File.Create(path);  

            writer.Serialize(file, epgList);  
            file.Close(); 
        }

        private string AddTimeshift(string time, string timeshift){
            var originalTimeshift = time.Substring(time.Length - 5, 5);
            var regExPattern = @"\+[0-9]+";

            if(!Regex.IsMatch(originalTimeshift, regExPattern) || !Regex.IsMatch(timeshift, regExPattern) ){
                return time;
            }
            
            return time.Substring(0, time.Length - 5) + timeshift;
        }
        
    }
}