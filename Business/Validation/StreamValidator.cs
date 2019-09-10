using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Couchpotato.Business.Logging;
using CouchpotatoShared.Channel;

namespace Couchpotato.Business.Validation{
    public class StreamValidator:IStreamValidator{
        private readonly ILogging _logging;

        public StreamValidator(ILogging logging){
            _logging = logging;
        }

        public bool ValidateStreamByUrl(string url){
            return CheckAvailability(url);
        }

        public bool ValidateSingleStream(Channel stream){
            return CheckAvailability(stream.Url);
        }

        public List<String> ValidateStreams(List<Channel> streams){
            var streamCount = streams.Count();
            var i = 0;
            var invalidStreams = new List<string>();

            foreach(var stream in streams.ToList()){
                if(!CheckAvailability(stream.Url)){
                    invalidStreams.Add(stream.TvgName);
                    streams.Remove(stream);
                }

                i++;
                _logging.PrintSameLine($"- Progress: {((decimal)i / (decimal)streamCount).ToString("0%")}");
            }

            return invalidStreams;
        }

        private bool CheckAvailability(string url){
            var request  = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            const int maxBytes = 512;
            request.AddRange(0, maxBytes-1);

            try{
                using(WebResponse response = request.GetResponse()){
                    request.Abort();
                    return true;
                }
            }catch(Exception){
                return false;
            }
        }
    }
}