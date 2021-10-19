using VideoOnDemand.Redis.Store.Abstract;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Threading.Tasks;

namespace VideoOnDemand.Redis
{
    public class RedisClient
    {
        private IRedisCacheClient _cacheClient;
        private IRedisDatabase _redisDatabase;
        private IRedisCacheConnectionPoolManager _poolManager;

        public RedisClient(IRedisCacheClient cacheClient, IRedisCacheConnectionPoolManager poolManager)
        {
            _cacheClient = cacheClient;
            _redisDatabase = _cacheClient.GetDbFromConfiguration();
            _poolManager = poolManager;
        }

        public IRedisDatabase GetDB() => _redisDatabase;

        public async Task Subscribe<T>(string channelName, Func<T, Task> handler) where T : IStoreEntry
        {
            await _redisDatabase.SubscribeAsync(channelName, handler);
        }

        public async Task Unsubscribe<T>(string channelName, Func<T, Task> handler) where T : IStoreEntry
        {
            await _redisDatabase.UnsubscribeAsync(channelName, handler);
        }

        public async Task Send<T>(string channelId, T message) where T : IStoreEntry
        {
            await _redisDatabase.PublishAsync(channelId, message);
        }
    }
}
