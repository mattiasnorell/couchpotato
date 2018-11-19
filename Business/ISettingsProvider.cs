using System.Collections.Generic;
using System.IO;
using Couchpotato.Models;

namespace Couchpotato.Business{
    public interface ISettingsProvider{
        Settings Load(string path);
    }
}