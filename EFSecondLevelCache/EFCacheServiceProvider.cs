using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using EFSecondLevelCache.Contracts;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Using HttpRuntime.Cache as a cache service. It works with both desktop and web applications.
    /// </summary>
    public class EFCacheServiceProvider : IEFCacheServiceProvider
    {
        private static readonly EFCacheKey _nullObject = new EFCacheKey();
        private static readonly SortedSet<string> _rootKeys = new SortedSet<string>();

        /// <summary>
        /// `HttpRuntime.Cache.Insert` won't accept null values.
        /// So we need a custom Null object here. It should be defined `static readonly` in your code.
        /// </summary>
        public object NullObject => _nullObject;

        /// <summary>
        /// Returns list of the cached keys.
        /// </summary>
        public IList<string> AllCachedKeys
        {
            get
            {
                var results = new List<string>();
                var enumerator = HttpRuntime.Cache.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    results.Add(enumerator.Key.ToString());
                }
                return results;
            }
        }

        /// <summary>
        /// Removes the cached entries added by this library.
        /// </summary>
        /// <param name="keyHashPrefix">Its default value is EF_.</param>
        public void ClearAllCachedEntries(string keyHashPrefix = EFCacheKey.KeyHashPrefix)
        {
            InvalidateCacheDependencies(_rootKeys.ToArray());

            var keys = GetAllEFCachedKeys(keyHashPrefix);
            foreach (var key in keys)
            {
                HttpRuntime.Cache.Remove(key);
            }
        }

        /// <summary>
        /// Gets all of the cached keys, added by this library.
        /// </summary>
        /// <param name="keyHashPrefix">Its default value is EF_.</param>
        /// <returns>list of the keys</returns>
        public IList<string> GetAllEFCachedKeys(string keyHashPrefix = EFCacheKey.KeyHashPrefix)
        {
            var results = new List<string>();
            var enumerator = HttpRuntime.Cache.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (!enumerator.Key.ToString().StartsWith(keyHashPrefix))
                    continue;

                results.Add(enumerator.Key.ToString());
            }

            return results;
        }

        /// <summary>
        /// Gets a cached entry by key.
        /// </summary>
        /// <param name="cacheKey">key to find</param>
        /// <returns>cached value</returns>
        public object GetValue(string cacheKey)
        {
            return HttpRuntime.Cache.Get(cacheKey);
        }

        /// <summary>
        /// Adds a new item to the cache.
        /// </summary>
        /// <param name="cacheKey">key</param>
        /// <param name="value">value</param>
        /// <param name="rootCacheKeys">cache dependencies</param>
        /// <param name="absoluteExpiration">absolute expiration time</param>
        /// <param name="priority">its default value is CacheItemPriority.Normal</param>
        public void InsertValue(string cacheKey, object value,
                                string[] rootCacheKeys,
                                DateTime absoluteExpiration,
                                CacheItemPriority priority = CacheItemPriority.Normal)
        {
            if (value == null)
            {
                value = NullObject; // `HttpRuntime.Cache.Insert` won't accept null values.
            }

            HttpRuntime.Cache.Insert(
                    key: cacheKey,
                    value: value,
                    dependencies: new CacheDependency(null, rootCacheKeys),
                    absoluteExpiration: absoluteExpiration,
                    slidingExpiration: Cache.NoSlidingExpiration,
                    priority: priority,
                    onRemoveCallback: null);
        }

        /// <summary>
        /// Invalidates all of the cache entries which are dependent on any of the specified root keys.
        /// </summary>
        /// <param name="rootCacheKeys">cache dependencies</param>
        public void InvalidateCacheDependencies(string[] rootCacheKeys)
        {
            foreach (var rootCacheKey in rootCacheKeys)
            {

                if (string.IsNullOrWhiteSpace(rootCacheKey)) continue;
                // Removes all cached items depend on this key.
                // If any of those cached items change, the whole dependency will be changed and the dependent item will be invalidated as well.
                HttpRuntime.Cache.Remove(rootCacheKey);
            }
        }

        /// <summary>
        /// The name of the cache keys used to clear the cache. All cached items depend on these keys.
        /// </summary>
        public void StoreRootCacheKeys(string[] rootCacheKeys)
        {
            foreach (var rootCacheKey in rootCacheKeys)
            {
                if (HttpRuntime.Cache.Get(rootCacheKey) != null)
                    continue;

                HttpRuntime.Cache.Add(
                    rootCacheKey,
                    rootCacheKey,
                    null,
                    Cache.NoAbsoluteExpiration,
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.Default,
                    null);

                _rootKeys.Add(rootCacheKey);
            }
        }
    }
}