using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Couchpotato.Models;
using Newtonsoft.Json;

public class SettingsProvider :  ISettingsProvider
{
    private readonly IFileHandler fileHandler;

    public SettingsProvider(IFileHandler fileHandler){
        this.fileHandler = fileHandler;
    }

     public Settings Load(string path)
    {
        Console.WriteLine("Loading settings from " + path);
        
        var file = this.fileHandler.GetSource(path);

        if(file == null){
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"- Couldn't load settingsfile from {path}");
            Console.ForegroundColor = ConsoleColor.White;
            return null;
        }

        using (StreamReader responseReader = new StreamReader(file))
            {
            var response = responseReader.ReadToEnd();
            return JsonConvert.DeserializeObject<Settings>(response);
        }
    }
}