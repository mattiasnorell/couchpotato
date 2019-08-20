using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Couchpotato.Business.Logging;
using CouchpotatoShared.Epg;
using Couchpotato.Business.Settings.Models;

namespace Couchpotato.Business
{
    public class EpgProvider: IEpgProvider{
        private readonly IFileHandler fileHandler;
        private readonly ILogging logging;

        public EpgProvider(
            IFileHandler fileHandler,
            ILogging logging
        ){
            this.fileHandler = fileHandler;
            this.logging = logging;
        }

        public EpgList GetProgramGuide(string[] paths, UserSettings settings){
            var loadedEpgLists = this.Load(paths);
            var filteredEpgList = this.Filter(loadedEpgLists, settings);

            return filteredEpgList;
        }

        public void Save(string path, EpgList epgList){
            this.logging.Print($"Writing EPG-file to {path}"); 
            var writer =  new XmlSerializer(typeof(EpgList));  
            var file = System.IO.File.Create(path);  

            writer.Serialize(file, epgList);  
            file.Close(); 
        }
        
        private EpgList Load(string[] paths){
            this.logging.Print($"\nLoading EPG-files:");

            var epgList = new EpgList();
            epgList.GeneratorInfoName = "";
            epgList.GeneratorInfoUrl = "";
            epgList.Channels = new List<EpgChannel>();
            epgList.Programs = new List<EpgProgram>();

            foreach(var path in paths){
                var epgFile = Parse(path);

                if(epgFile == null){
                    this.logging.Error($"- Couldn't download file {path}");
                    continue;
                }

                epgList.Channels.AddRange(epgFile.Channels);
                epgList.Programs.AddRange(epgFile.Programs);
            }    

            return epgList;
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

                try{
                    return (EpgList)serializer.Deserialize(stream);          
                }catch(Exception ex){
                    this.logging.Error("Couldn't deserialize the EPG-list", ex);
                    return null;
                }
            };
        }

        private EpgList Filter(EpgList input, UserSettings settings){
            var epgFile = new EpgList();
            epgFile.GeneratorInfoName = "Couchpotato";
            epgFile.Channels = new List<EpgChannel>();
            epgFile.Programs = new List<EpgProgram>();

            var channelCount = settings.Channels.Count;
            var i = 0;

            var missingChannels = new List<UserSettingsChannel>();

            foreach(var settingsChannel in settings.Channels){
                i = i + 1;
                var epgId = settingsChannel.EpgId ?? settingsChannel.ChannelId;
                var channel = input.Channels.FirstOrDefault(e => e.Id == epgId);

                this.logging.PrintSameLine("\rFiltering EPG-files: " + ((decimal)i / (decimal)channelCount).ToString("0%"));

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

            this.logging.Print("\n"); //TODO: Remove this and make better

            if(missingChannels.Any()){
                Console.ForegroundColor = ConsoleColor.Red;
                this.logging.Warn($"Couldn't find EPG for:");
                
                foreach(var missingChannel in missingChannels){
                    this.logging.Warn($"- { missingChannel.FriendlyName}");
                }

                Console.ForegroundColor = ConsoleColor.White;
            }

            return epgFile;
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