using System.Collections.Generic;
using System.IO;
using Couchpotato.Models;

public interface ISettingsProvider{
    Settings Load(string path);
}