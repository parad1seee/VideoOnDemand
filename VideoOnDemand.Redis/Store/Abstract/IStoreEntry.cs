using StackExchange.Redis;

namespace VideoOnDemand.Redis.Store.Abstract
{
    public interface IStoreEntry
    {
        HashEntry[] GetEntry();
    }
}
