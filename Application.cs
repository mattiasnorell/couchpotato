
using System;
using System.IO;
using System.Linq;

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

        if(settings == null){
            Console.WriteLine($"\nNeed settings. Please fix. Thanks.");
            
            Environment.Exit(0);
        }

        var channels = channelProvider.Load(settings.M3uPath, settings);

        if(!channels.Any()){
            Console.WriteLine($"\nNo channels found so no reason to continue. Bye bye.");
            
            Environment.Exit(0);
        }

        var epgFile = epgProvider.Load(settings.EpgPath, settings);
        var outputPath = settings.OutputPath ?? "./";
       
        if(!Directory.Exists(outputPath)){
            Console.WriteLine($"Couldn't find output folder, creating it at {outputPath}!");
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