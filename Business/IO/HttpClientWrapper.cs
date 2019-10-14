using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Threading;

namespace Couchpotato.Business.IO
{
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;

        public HttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Stream> Get(string url)
        {
            var request = new HttpRequestMessage { RequestUri = new Uri(url) };

            try
            {
                var response = await _httpClient.SendAsync(request);
                var stream = response.Content.ReadAsStreamAsync().Result;

                return stream;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> Validate(string url, string[] mediaTypes)
        {
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                
                if(!response.IsSuccessStatusCode){
                    return false;
                }
                
                if(mediaTypes.Length > 0 && !mediaTypes.Any(e => e == response.Content.Headers.ContentType.MediaType)){
                    return false;
                }
                
                
                return true; 
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}