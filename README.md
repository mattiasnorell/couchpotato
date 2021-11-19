# Couchpotato
Customize and aggregate M3U and EPG files

## Application settings (appsettings.json)
```json
{
    "pluginPath" : "PATH_TO_PLUGIN_FOLDER",
    "plugins": {
        "PluginName": {
            "Setting": "lorem ipsum dolor sit amet"
        }
    }
}
```

## Project settings
```json
{
    "outputPath": "Relative or absolute path to output folder",
    "m3uPath": "Path to m3u source",
    "m3uCacheDuration": 1,
    "epg" : {
        "cache": {
            "enabled": true,
            "lifespan": 24
        },
        "paths": [
            "Path to epg source 1",
            "Path to epg source 2",
            "etc..."
        ]
    },
    "compress": true,
    "validation": {
        "enabled": true,
        "contentTypes" :[
            "text/html",
            "audio/ogg"
        ],
        "defaultFallbacks": [
            {
                "key": " FOO ",
                "value": [
                    " BAR ", 
                    " "
                ]
            }
        ],
    },
    "defaultGroup": "Group title",
    "groups": [
       {
        "groupId": "Group id",
        "friendlyName": "Friendly name",
        "exclude": [
                "tvg-name",
                "tvg-name"
            ]
        }
    ],
    "streams": [
    {
        "channelId": "Channel id",
        "epgId":"epg-channel-id",
        "epgTimeshift": "+0000",
        "friendlyName": "Friendly name",
        "customGroupName":"Group title",
        "fallbacks": [
            "channelId of fallback channel",
            "channelId of fallback channel",
            "...",
        ]
    }
    ]
}
```

## How to compile/publish
There is a lot more to this and others can explain it way better than me, but here are the basic commands. 
Read more about publishing DotNet Core applications [here](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) and [here](https://docs.microsoft.com/en-us/dotnet/core/deploying/deploy-with-cli).

#### RaspberryPi
```
dotnet publish -c Release -r linux-arm
```

#### Windows
```
dotnet publish -c Release -r win10-x64
```

#### MacOS
```
dotnet publish -c Release -r osx.10.11-x64
```

#### Linux/Ubuntu
```
dotnet publish -c Release -r ubuntu.14.04-x64
```

## How to run
#### Linux / Debian / MacOS
* Copy all files from the "bin/Release/netcoreapp2.1/your platform/publish/". 
* Make Couchpotato executable: chmod +x /path/to/couchpotato.
* The settings file can be either a local or http path. You can use multiple settings files.

#### Windows
No need for any additional configuration besides compiling.
```
/path/to/couchpotato /path/to/settings.json /path/to/additional/settings.json
```


## Add as scheduled job

#### Linux / Debian
* Make sure Couchpotato can run before continuing.
* Add the line below to Cron. The settings file can be either a local or http path. Local paths have to be absolute and not relative to the application.
* Type "crontab -e" to edit the crontab for the current user.
```
0 4 * * * /path/to/couchpotato /path/to/settings.json
```

## Plugins
> The plugin system in Couchpotato is at the moment **very** rudimentary. If you use external DLLs it will only load that file, not check for dependencies or assembly version missmatchs. This **might** change in future versions.

Couchpotatos real job is to read and write m3u and epg-files. That's it. No more functionallity should or will be added to the core application. "But I need this and that!" you might think. Don't worry, I got you fam. Enter, plugins!

Create a class, either in the Couchpotato project or as a new project, and have it implement the IPlugin-interface. Now Couchpotato will discover it. Then add the CouchpotatoPlugin-attribute and set when you want the plugin to run. Avaliable 

| Lifecycle event | Description | 
| :------------- | :------------- |
| ApplicationStart | Once the application is finished bootstrapping this is the first thing that will run. |
| BeforeChannel | Will run before all M3U-files are loaded and parsed. Good place to run custom channel stuff. |
| AfterChannel | Will run after all M3U-files are loaded and parsed. Good place to run custom channel modification stuff. |
| BeforeEpg | Will run before all EPG-files are loaded and parsed. Good place to run custom EPG stuff. |
| AfterEpg | Will run after all EPG-files are loaded and parsed. Good place to run custom epg modification stuff. |
| ApplicationFinished | The last thing that will happen before the application exits. Good place to remove temp-files etc. |

#### Boilerplate
```
[CouchpotatoPlugin(PluginType.ApplicationStart)]
public class HelloWorldPlugin: IPlugin {
    public void Run(){
        Console.WriteLine("Hello World!");
    }
}
```
