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
class Application: IApplication{
    private readonly ISettingsProvider settingsProvider;
    private readonly IChannelProvider channelProvider;
    private readonly IEpgProvider epgProvider;
    private readonly ICompression compression;

    public Application(ISettingsProvider settingsProvider, IChannelProvider channelProvider, IEpgProvider epgProvider, ICompression compression){
        this.settingsProvider = settingsProvider;
        this.channelProvider = channelProvider;
        this.epgProvider = epgProvider;
        this.compression = compression;
    }

    public void Run(string settingsPath){
        var settings = settingsProvider.Load(settingsPath);
        var channels = channelProvider.Load(settings.M3uPath, settings);
        var epgFile = epgProvider.Load(settings.EpgPath, settings);
        var outputPath = settings.OutputPath ?? "./";

        if(!Directory.Exists(outputPath)){
            Directory.CreateDirectory(outputPath); 
        }

        var outputM3uPath = Path.Combine(outputPath, "channels.m3u");
        channelProvider.Save(outputM3uPath, channels);

        var outputEpgPath = Path.Combine(outputPath, "epg.xml");
        epgProvider.Save(outputEpgPath, epgFile);
        

        if(settings.Compress){
            compression.Compress(outputM3uPath);
            compression.Compress(outputEpgPath);
        }

        Console.WriteLine("Done!");
    }         
}