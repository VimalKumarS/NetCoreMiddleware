using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace WebApplication_LearnMiddleware
{
    public sealed class Cache : CacheBase
    {
        private readonly IMemoryCache _cache;

        public Cache(IMemoryCache cache)
        {
            _cache = cache;
        }
        protected override bool TryFind<TResult>(string key, CachePolicy policy, out TResult value)
        {
            var item = this._cache.Get(key);
            if (item != null)
            {
                value = (TResult)item;
                return true;
            }

            value = default(TResult);
            return false;
        }

        protected override void Add<TResult>(string key, TResult value, CachePolicy policy)
        {
            //var cacheItem = new CacheItem(key, value);
            var cachePolicy = new MemoryCacheEntryOptions();
            if (policy.RenewLeaseOnAccess)
            {
                cachePolicy.SlidingExpiration = policy.ExpiresAfter;
            }
            else
            {
                cachePolicy.AbsoluteExpiration = DateTimeOffset.UtcNow + policy.ExpiresAfter;
            }

            this._cache.Set(key, value, cachePolicy);
        }
    }
}

