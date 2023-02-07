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

            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");
        }

        public async Task<Stream> Get(string url)
        {
            var request = new HttpRequestMessage { RequestUri = new Uri(url) };

            try
            {
                var response = await _httpClient.SendAsync(request);
                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<bool> Validate(string url, string[] mediaTypes, int minimumContentLength = 100000)
        {
            try
            {
                using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var shouldContinue = true;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        if (mediaTypes.Length > 0 &&
                            !mediaTypes.Any(e => e == response.Content.Headers.ContentType.MediaType))
                        {
                            return false;
                        }

                        var bytesRead = 0L;
                        var buffer = new byte[8192];

                        do
                        {
                            var bytes = await contentStream.ReadAsync(buffer);

                            if (bytes == 0)
                            {
                                shouldContinue = false;

                                return false;
                            }
                            else if (bytesRead >= minimumContentLength)
                            {
                                shouldContinue = false;
                                return true;
                            }
                            else
                            {
                                bytesRead += bytes;
                            }
                        } while (shouldContinue);
                    }
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