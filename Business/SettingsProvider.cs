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

        if(!File.Exists(path)){
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"- Couldn't find file {path}");
            Console.ForegroundColor = ConsoleColor.White;
            return null;
        }

        using (StreamReader responseReader = new StreamReader(path))
        {
            string response = responseReader.ReadToEnd();
            settings = JsonConvert.DeserializeObject<Settings>(response);
        }

        return settings;
    }

}