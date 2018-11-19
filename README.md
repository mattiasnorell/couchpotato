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
    "validateStreams": false,
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
        "customGroupName":"Group title",
        "fallbackChannels": [
            "channelId of fallback channel",
            "channelId of fallback channel",
            "...",
        ]
    }
    ]
}
````

| Property | Description | Type | Required |
| :------------- | :------------- |:------------- |:------------- |
| outputPath | Local path to where the generated files will be saved. Path can be either relative or absolute. | String | Yes |
| m3uPath | Path to the source m3u-file. Local or http(s). | String | Yes |
| validateStreams | Check that streams are ok | Boolean (default: false) | no |
| epgPath | List of EPG-files (.xml or .xml.gz). Can be either local (relative or absolute) or http(s) path.| String | No |
| compress | If you want the output files to be compressed. | Boolean (default: false) | No |
| defaultChannelGroup | The groupname used for all channels without a groupname or the "CustomChannelGroup" property. | String | No |
| groups | Import all items from a group. | Array | No |
| channels | Import individual items. | Array | No |

