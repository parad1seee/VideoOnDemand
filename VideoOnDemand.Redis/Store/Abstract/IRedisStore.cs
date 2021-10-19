using System.Collections.Generic;
using System.Threading.Tasks;

namespace VideoOnDemand.Redis.Store.Abstract
{
    public interface IRedisStore<T> where T : IStoreEntry
    {
        Task<T> Get(string key);

        Task<IDictionary<string, T>> Get(List<string> keys);

        Task<IDictionary<string, T>> GetByPattern(string pattern);

        Task DeleteByPattern(string pattern);

        Task Delete(string key);

        Task Set(string key, T value);

        Task<IEnumerable<string>> GetKeys(string pattern);
    }
}
