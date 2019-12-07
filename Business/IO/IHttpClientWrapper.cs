using System.IO;
using System.Threading.Tasks;

namespace Couchpotato.Business.IO{ 
    public interface IHttpClientWrapper{
        Task<Stream> Get(string url);
        Task<bool> Validate(string url, string[] mediaTypes, int minimumContentLength = 100000);
    }
}