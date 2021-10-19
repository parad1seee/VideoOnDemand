using VideoOnDemand.Redis.Store.Abstract;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VideoOnDemand.Redis.Store
{
    public class RedisStore<T> : IRedisStore<T> where T : IStoreEntry
    {
        private readonly IRedisDatabase _db;

        public RedisStore(IRedisCacheClient cacheClient)
        {
            _db = cacheClient.GetDbFromConfiguration();
        }

        public async Task Delete(string key)
        {
            try
            {
                if (!(await _db.ExistsAsync(key)))
                    throw new ArgumentNullException("entity");

                await _db.RemoveAsync(key);
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public async Task DeleteByPattern(string pattern)
        {
            try
            {
                var keys = await GetKeys(pattern);

                if (keys != null && keys.Any())
                    await _db.RemoveAllAsync(keys);
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public async Task<T> Get(string key)
        {
            try
            {
                var result = await _db.GetAsync<T>(key);

                return result;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public async Task<IDictionary<string, T>> Get(List<string> keys)
        {
            try
            {
                var result = await _db.GetAllAsync<T>(keys);

                return result;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public async Task<IDictionary<string, T>> GetByPattern(string pattern)
        {
            try
            {
                var keys = await GetKeys(pattern);

                var result = await _db.GetAllAsync<T>(keys);

                return result;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetKeys(string pattern)
        {
            try
            {
                var result = await _db.SearchKeysAsync(pattern);

                return result;
            }
            catch (Exception exception)
            {
                throw;
            }
        }

        public async Task Set(string key, T value)
        {
            try
            {
                await _db.AddAsync(key, value);
            }
            catch (Exception exception)
            {
                throw;
            }
        }
    }
}
