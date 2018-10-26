using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

public class ProviderBase
{
    public Stream DownloadFile(string path){
        using (var client = new WebClient())
        {
            try{
                return client.OpenRead(path);
            }catch (Exception)
            {
                return null;
            }
        }
    }

    public string GetValueForAttribute(string item, string attributeName){
        var result = new Regex(attributeName + @"=\""([^""]*)\""", RegexOptions.Singleline).Match(item);
        
        if(result == null || result.Groups.Count < 1){
            return string.Empty;
        }
        
        return result.Groups[1].Value;
    }
}