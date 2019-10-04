using System.IO;

namespace Couchpotato.Business.Cache{
    public interface ICacheProvider
    {
        Stream Get(string key, int timespan);
        void Set(string key, Stream stream);
    }
}