using VideoOnDemand.Redis.Store.Abstract;
using VideoOnDemand.Redis.Utils;
using StackExchange.Redis;

namespace VideoOnDemand.Redis.Models.Abstract
{
    public abstract class RedisRecord : IStoreEntry
    {
        public virtual HashEntry[] GetEntry()
        {
            return this.ToHashEntries();
        }
    }
}
