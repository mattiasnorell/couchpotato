using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Couchpotato.Business.Logging;
using Couchpotato.Core.Epg;
using Couchpotato.Business.Settings.Models;
using Couchpotato.Business.IO;
using Couchpotato.Business.Cache;
using Couchpotato.Business.Settings;

namespace Couchpotato.Business
{
    public class EpgProvider : IEpgProvider
    {
        private readonly IFileHandler _fileHandler;
        private readonly ILogging _logging;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISettingsProvider _settingsProvider;

        public EpgProvider(
            IFileHandler fileHandler,
            ILogging logging,
            ICacheProvider cacheProvider,
            ISettingsProvider settingsProvider
        )
        {
            _fileHandler = fileHandler;
            _logging = logging;
            _cacheProvider = cacheProvider;
            _settingsProvider = settingsProvider;
        }

        public EpgResult GetProgramGuide(string[] paths)
        {
            var loadedEpgLists = Load(paths);
            var filteredEpgList = Filter(loadedEpgLists);

            return filteredEpgList;
        }

        public string Save(string path, string fileName, EpgList epgList)
        {
            return _fileHandler.WriteXmlFile<EpgList>(path, fileName, epgList);
        }

        private EpgList Load(string[] paths)
        {
            _logging.Print($"\nLoading EPG-files:");

            var epgList = new EpgList()
            {
                GeneratorInfoName = "",
                GeneratorInfoUrl = "",
                Channels = new List<EpgChannel>(),
                Programs = new List<EpgProgram>()
            };

            foreach (var path in paths)
            {
                var epgFile = Parse(path);

                if (epgFile == null)
                {
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
            using (var stream = _fileHandler.GetSource(path))
            {

                var streamValue = stream;

                if (streamValue == null)
                {

                    if (!_settingsProvider.Epg.Cache.Enabled)
                    {
                        return null;
                    }

                    streamValue = _cacheProvider.Get(path, _settingsProvider.Epg.Cache.Lifespan);

                    if (streamValue == null)
                    {
                        return null;
                    }

                    _logging.Info($"  Using cached value for {path}");
                }

                try
                {
                    var xRoot = new XmlRootAttribute()
                    {
                        ElementName = "tv",
                        IsNullable = true
                    };

                    var serializer = new XmlSerializer(typeof(EpgList), xRoot);
                    var returnValue = serializer.Deserialize(streamValue) as EpgList;

                    if (_settingsProvider.Epg.Cache.Enabled)
                    {
                        _cacheProvider.Set(path, streamValue);
                    }

                    return returnValue;
                }
                catch (Exception ex)
                {
                    _logging.Error("Couldn't deserialize the EPG-list", ex);
                    return null;
                }
            };
        }

        private EpgResult Filter(EpgList input)
        {
            var i = 0;
            var channelCount = _settingsProvider.Streams.Count;
            var result = new EpgResult();
            var epgFile = new EpgList()
            {
                GeneratorInfoName = "",
                GeneratorInfoUrl = "",
                Channels = new List<EpgChannel>(),
                Programs = new List<EpgProgram>()
            };

            var streamsWithoutEpg = new List<UserSettingsStream>();

            foreach (var settingsChannel in _settingsProvider.Streams)
            {
                i = i + 1;
                _logging.Progress($"Filtering EPG-files", i, channelCount);

                var epgId = settingsChannel.EpgId ?? settingsChannel.ChannelId;
                var channel = input.Channels.FirstOrDefault(e => e.Id == epgId);

                if (channel == null)
                {
                    streamsWithoutEpg.Add(settingsChannel);
                    continue;
                }

                var epgChannel = new EpgChannel()
                {
                    Id = channel.Id,
                    DisplayName = settingsChannel.FriendlyName ?? channel.DisplayName,
                    Url = channel.Url
                };

                epgFile.Channels.Add(epgChannel);

                foreach (var program in input.Programs.Where(e => e.Channel == channel.Id))
                {
                    var epgProgram = new EpgProgram()
                    {
                        Channel = program.Channel,
                        Desc = program.Desc,
                        EpisodeNumber = program.EpisodeNumber,
                        Lang = program.Lang,
                        Start = string.IsNullOrEmpty(settingsChannel.EpgTimeshift) ? program.Start : AddTimeshift(program.Start, settingsChannel.EpgTimeshift),
                        Stop = string.IsNullOrEmpty(settingsChannel.EpgTimeshift) ? program.Stop : AddTimeshift(program.Stop, settingsChannel.EpgTimeshift),
                        Title = program.Title
                    };

                    epgFile.Programs.Add(epgProgram);
                }
            }

            if (streamsWithoutEpg.Any())
            {
                _logging.Warn($"Couldn't find EPG for:");

                foreach (var streamWithoutEpg in streamsWithoutEpg)
                {
                    result.StreamsWithoutEpg.Add(streamWithoutEpg.ChannelId);
                    _logging.Warn($"- { streamWithoutEpg.FriendlyName}");
                }
            }

            result.Items = epgFile;
            
            return result;
        }

        private string AddTimeshift(string time, string timeshift)
        {
            var originalTimeshift = time.Substring(time.Length - 5, 5);
            var regExPattern = @"\+[0-9]+";

            if (!Regex.IsMatch(originalTimeshift, regExPattern) || !Regex.IsMatch(timeshift, regExPattern))
            {
                return time;
            }

            return time.Substring(0, time.Length - 5) + timeshift;
        }
    }
}