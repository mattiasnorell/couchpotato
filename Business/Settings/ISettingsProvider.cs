using System.Collections.Generic;
using Couchpotato.Business.Settings.Models;

namespace Couchpotato.Business.Settings
{
    public interface ISettingsProvider
    {
        bool Load(string path);
        string Source { get; }
        string DefaultGroup { get; }
        string OutputPath { get; }
        string OutputFilename { get; }
        bool Compress { get; }
        int PlaylistCacheDuration { get; }
        List<UserSettingsStream> Streams { get; }
        List<UserSettingsGroup> Groups { get; }
        UserSettingsValidation Validation { get; }
        UserSettingsEpg Epg { get; }
    }
}