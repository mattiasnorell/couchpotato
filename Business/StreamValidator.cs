using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Couchpotato.Business.Logging;
using Couchpotato.Models;
using CouchpotatoShared.Channel;

namespace Couchpotato.Business{
    public class StreamValidator:IStreamValidator{
        private readonly ILogging logging;

        public StreamValidator(ILogging logging){
            this.logging = logging;
        }

        public bool ValidateStreamByUrl(string url){
            return CheckChannelAvailability(url);
        }

        public bool ValidateSingleStream(Channel stream){
            return CheckChannelAvailability(stream.Url);
        }

        public List<String> ValidateStreams(List<Channel> streams){
            var streamCount = streams.Count();
            var i = 0;
            var invalidStreams = new List<string>();

            foreach(var stream in streams.ToList()){
                if(!CheckChannelAvailability(stream.Url)){
                    invalidStreams.Add(stream.TvgName);
                    streams.Remove(stream);
                }

                i++;
                this.logging.PrintSameLine($"- Progress: {((decimal)i / (decimal)streamCount).ToString("0%")}");
            }

            return invalidStreams;
        }

        private bool CheckChannelAvailability(string url){
            var request  = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            const int maxBytes = 1024;
            request.AddRange(0, maxBytes-1);

            try{
                using(WebResponse response = request.GetResponse()){
                    request.Abort();
                    return true;
                }
            }catch(Exception ex){
                this.logging.Error("Channel validation failed", ex);
                return false;
            }
        }
    }
}