using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Couchpotato.Business.Logging;
using CouchpotatoShared.Epg;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Business.IO;

namespace Couchpotato.Business
{
    public class EpgProvider: IEpgProvider{
        private readonly IFileHandler _fileHandler;
        private readonly ILogging _logging;

        public EpgProvider(
            IFileHandler fileHandler,
            ILogging logging
        ){
            _fileHandler = fileHandler;
            _logging = logging;
        }

        public EpgList GetProgramGuide(string[] paths, UserSettings settings){
            var loadedEpgLists = this.Load(paths);
            var filteredEpgList = this.Filter(loadedEpgLists, settings);

            return filteredEpgList;
        }

        public string Save(string path, string fileName, EpgList epgList){
            return _fileHandler.WriteXmlFile<EpgList>(path, fileName, epgList);
        }
        
        private EpgList Load(string[] paths){
            _logging.Print($"\nLoading EPG-files:");

            var epgList = new EpgList(){
                GeneratorInfoName = "",
                GeneratorInfoUrl = "",
                Channels = new List<EpgChannel>(),
                Programs = new List<EpgProgram>()
            };

            foreach(var path in paths){
                var epgFile = Parse(path);

                if(epgFile == null){
                    _logging.Error($"- Couldn't download file {path}");
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

            using(var stream = _fileHandler.GetSource(path)){
                if(stream == null){
                    return null;
                }

                try{
                    return (EpgList)serializer.Deserialize(stream);          
                }catch(Exception ex){
                    _logging.Error("Couldn't deserialize the EPG-list", ex);
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

                _logging.Progress($"Filtering EPG-files", i, channelCount);

                if(channel == null){
                    missingChannels.Add(settingsChannel);
                    continue;
                }

                var epgChannel = new EpgChannel(){
                    Id = channel.Id,
                    DisplayName = settingsChannel.FriendlyName ?? channel.DisplayName,
                    Url = channel.Url
                };

                epgFile.Channels.Add(epgChannel);

                foreach(var program in input.Programs.Where(e=> e.Channel == channel.Id)){
                    var epgProgram = new EpgProgram(){
                        Channel = program.Channel,
                        Desc = program.Desc,
                        EpisodeNumber = program.EpisodeNumber,
                        Lang = program.Lang,
                        Start = string.IsNullOrEmpty(settingsChannel.EpgTimeshift) ? program.Start : AddTimeshift(program.Start, settingsChannel.EpgTimeshift),
                        Stop =  string.IsNullOrEmpty(settingsChannel.EpgTimeshift) ? program.Stop : AddTimeshift(program.Stop, settingsChannel.EpgTimeshift),
                        Title = program.Title
                    };

                    epgFile.Programs.Add(epgProgram);
                }
            }

            if(missingChannels.Any()){
                Console.ForegroundColor = ConsoleColor.Red;
                _logging.Warn($"Couldn't find EPG for:");
                
                foreach(var missingChannel in missingChannels){
                    _logging.Warn($"- { missingChannel.FriendlyName}");
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