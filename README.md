# Couchpotato
Customize and aggrigate M3U and EPG files

## How to run
```
couchpotato ./path-to-settings.json
```

## Settings
```json
{
    "outputPath": "Relative or absolute path to output folder",
    "m3uPath": "Path to m3u source",
    "epgPath" : [
        "Path to epg source 1",
        "Path to epg source 2",
        "etc..."
    ],
    "compress": true,
    "defaultChannelGroup": "Group title",
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
    "channels": [
    {
        "channelId": "Channel id",
        "epgId":"epg-channel-id",
        "friendlyName": "Friendly name",
        "customGroupName":"Group title"
    }
    ]
}
```

| Property        | Description           |
| ------------- |-------------:|
| outputPath      | Where the generated files will be saved. Path can be either relative or absolute. |
| m3uPath      | Path to the source m3u-file. Can be either relative or absolute. |
| epgPath | List of EPG-files. Can be either local (relative or absolute) or http path. |
| compress | If you want the output files to be compressed. |
| defaultChannelGroup      | . |
| groups      | Import all items from a group. |
| channels | Import individual items. |