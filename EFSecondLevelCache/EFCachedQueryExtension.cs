using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using EFSecondLevelCache.Contracts;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Returns a new cached query.
    /// </summary>
    public static class EFCachedQueryExtension
    {
        private static readonly IEFCacheKeyProvider _defaultCacheKeyProvider;
        private static readonly IEFCacheServiceProvider _defaultCacheServiceProvider;
        private static readonly IEFCacheKeyProvider _defaultLinqToObjectsCacheKeyProvider;

        static EFCachedQueryExtension()
        {
            _defaultCacheServiceProvider = new EFCacheServiceProvider();
            _defaultCacheKeyProvider = new EFCacheKeyProvider(new EFCacheKeyHashProvider());
            _defaultLinqToObjectsCacheKeyProvider = new LinqToObjectsCacheKeyProvider(new EFCacheKeyHashProvider());
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <param name="efCachePolicy">Determines the AbsoluteExpiration time and Priority of the cache.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        /// <returns></returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCachePolicy efCachePolicy, EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider, IEFCacheServiceProvider cacheServiceProvider)
        {
            var noTrackingQuery = query.toAsNoTrackingQuery();
            if (isLinqToObjectsQuery(noTrackingQuery))
            {
                return new EFCachedQueryable<TType>(
                    query, efCachePolicy, debugInfo, _defaultLinqToObjectsCacheKeyProvider, cacheServiceProvider);
            }
            return new EFCachedQueryable<TType>(
                noTrackingQuery, efCachePolicy, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        ///  <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        ///  </summary>
        ///  <typeparam name="TType">Entity type.</typeparam>
        ///  <param name="query">The input EF query.</param>
        ///  <param name="efCachePolicy">Determines the AbsoluteExpiration time and Priority of the cache.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCachePolicy efCachePolicy, EFCacheDebugInfo debugInfo)
        {
            return Cacheable(query, efCachePolicy, debugInfo, _defaultCacheKeyProvider, _defaultCacheServiceProvider);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        /// </summary>
        /// <typeparam name="TType">Entity type.</typeparam>
        /// <param name="query">The input EF query.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query)
        {
            return Cacheable(query, new EFCachePolicy(), new EFCacheDebugInfo());
        }

        ///  <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        ///  </summary>
        ///  <typeparam name="TType">Entity type.</typeparam>
        ///  <param name="query">The input EF query.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(this IQueryable<TType> query, EFCacheDebugInfo debugInfo)
        {
            return Cacheable(query, new EFCachePolicy(), debugInfo);
        }

        ///  <summary>
        /// Returns a new query where the entities returned will be cached in the IEFCacheServiceProvider.
        ///  </summary>
        ///  <typeparam name="TType">Entity type.</typeparam>
        ///  <param name="query">The input EF query.</param>
        ///  <param name="efCachePolicy">Determines the AbsoluteExpiration time and Priority of the cache.</param>
        /// <returns>Provides functionality to evaluate queries against a specific data source.</returns>
        public static EFCachedQueryable<TType> Cacheable<TType>(
            this IQueryable<TType> query, EFCachePolicy efCachePolicy)
        {
            return Cacheable(query, efCachePolicy, new EFCacheDebugInfo());
        }

        private static bool isLinqToObjectsQuery<TType>(IQueryable<TType> noTrackingQuery)
        {
            return noTrackingQuery == null;
        }

        /// <summary>
        /// Returns a new query where the entities returned will not be cached in the DbContext.
        /// </summary>
        private static IQueryable<TType> toAsNoTrackingQuery<TType>(this IQueryable<TType> query)
        {
            var originalObjectQuery = query as ObjectQuery<TType>;
            if (originalObjectQuery != null)
            {
                originalObjectQuery.MergeOption = MergeOption.NoTracking;
                return query;
            }

            var dbQuery = query as DbQuery<TType>;
            return dbQuery == null ? null : dbQuery.AsNoTracking();
        }
    }
}