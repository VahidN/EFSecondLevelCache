using System;
using System.Collections.Generic;
using System.Web.Caching;

namespace EFSecondLevelCache.Contracts
{
    /// <summary>
    /// Cache Service Provider Contract.
    /// </summary>
    public interface IEFCacheServiceProvider
    {
        /// <summary>
        /// Returns list of the cached keys.
        /// </summary>
        IList<string> AllCachedKeys { get; }

        /// <summary>
        /// Removes the cached entries added by this library.
        /// </summary>
        /// <param name="keyHashPrefix">Its default value is EF_.</param>
        void ClearAllCachedEntries(string keyHashPrefix = EFCacheKey.KeyHashPrefix);

        /// <summary>
        /// Gets all of the cached keys, added by this library.
        /// </summary>
        /// <param name="keyHashPrefix">Its default value is EF_.</param>
        /// <returns>list of the keys</returns>
        IList<string> GetAllEFCachedKeys(string keyHashPrefix = EFCacheKey.KeyHashPrefix);

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="cacheKey">key to find</param>
        /// <returns>cached value</returns>
        object GetValue(string cacheKey);

        /// <summary>
        /// Adds a new item to the cache.
        /// </summary>
        /// <param name="cacheKey">key</param>
        /// <param name="value">value</param>
        /// <param name="rootCacheKeys">cache dependencies</param>
        /// <param name="absoluteExpiration">absolute expiration time</param>
        /// <param name="priority">its default value is CacheItemPriority.Normal</param>
        void InsertValue(string cacheKey, object value,
            string[] rootCacheKeys,
            DateTime absoluteExpiration,
            CacheItemPriority priority = CacheItemPriority.Normal);

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="rootCacheKeys">cache dependencies</param>
        void InvalidateCacheDependencies(string[] rootCacheKeys);

        /// <summary>
        /// The name of the cache keys used to clear the cache. All cached items depend on these keys.
        /// </summary>
        /// <param name="rootCacheKeys">cache dependencies</param>
        void StoreRootCacheKeys(string[] rootCacheKeys);

        /// <summary>
        /// `HttpRuntime.Cache.Insert` won't accept null values.
        /// So we need a custom Null object here. It should be defined `static readonly` in your code.
        /// </summary>
        object NullObject { get; }
    }
}