# Couchpotato
Customize and aggrigate M3U and EPG files

## How to run
```
filesorter ./path-to-settings.json
```

## Settings
```json
{
    "outputPath": "Relative or absolut path to output folder",
    "m3uPath": "Path to m3u source",
    "epgPath" : [
        "Path to epg source 1",
        "Path to epg source 2",
        "etc..."
    ],
    "compress": true,
    "defaultChannelGroup": "Group title",
    "groups": [
       "Group title 1",
       "Group title 2"
    ],
    "channels": [
    {
        "channelId": "Channel id",
        "epgId":"epg-channel-id",
        "friendlyName": "Friendly name",
        "group":"Group title"
    }
    ]
}
```