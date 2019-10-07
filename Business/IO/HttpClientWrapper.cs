using System;
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

        public async Task<bool> Validate(string url)
        {
            const int maxBytes = 512;

            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var bytes = new byte[maxBytes];
                    var bytesread = stream.Read(bytes, 0, bytes.Length);
                    stream.Close();
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