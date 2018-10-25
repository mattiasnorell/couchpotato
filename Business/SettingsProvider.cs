using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Couchpotato.Models;
using Newtonsoft.Json;

public class SettingsProvider : ISettingsProvider
{
     public Settings Load(string path)
    {
        Console.WriteLine("Loading settings from " + path);
        Settings settings;

        using (StreamReader responseReader = new StreamReader(path))
        {
            string response = responseReader.ReadToEnd();
            settings = JsonConvert.DeserializeObject<Settings>(response);
        }

        return settings;
    }

}