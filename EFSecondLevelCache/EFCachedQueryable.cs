using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EFSecondLevelCache.Contracts;
#if !NET40
using System.Data.Entity.Infrastructure;
#endif

namespace EFSecondLevelCache
{
    /// <summary>
    /// Provides functionality to evaluate queries against a specific data source.
    /// </summary>
    /// <typeparam name="TType">Type of the entity.</typeparam>
    public class EFCachedQueryable<TType> : IQueryable<TType>
#if !NET40
, IDbAsyncEnumerable<TType>
#endif
    {
        private readonly IQueryable<TType> _query;
        private readonly EFCachedQueryProvider<TType> _provider;

        /// <summary>
        /// Provides functionality to evaluate queries against a specific data source.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="efCache">EFCachePolicy determines the AbsoluteExpiration time and Priority of the cache.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">Cache Service Provider.</param>
        public EFCachedQueryable(
            IQueryable<TType> query,
            EFCachePolicy efCache,
            EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCacheServiceProvider cacheServiceProvider)
        {
            _query = query;
            _provider = new EFCachedQueryProvider<TType>(query, efCache, debugInfo, cacheKeyProvider, cacheServiceProvider);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        public IEnumerator<TType> GetEnumerator()
        {
            return ((IEnumerable<TType>)_provider.Materialize(_query.Expression, () => _query.ToArray())).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>A collections that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_provider.Materialize(_query.Expression, () => _query.ToArray())).GetEnumerator();
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of System.Linq.IQueryable is executed.
        /// </summary>
        public Type ElementType
        {
            get { return _query.ElementType; }
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of System.Linq.IQueryable.
        /// </summary>
        public Expression Expression
        {
            get { return _query.Expression; }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        public IQueryProvider Provider
        {
            get { return _provider; }
        }


        #region IDbAsyncEnumerable implementation
#if !NET40
        /// <summary>
        /// Returns an IDbAsyncEnumerator of TType which when enumerated will execute the query against the database.
        /// </summary>
        /// <returns>An enumerator for the query</returns>
        public IDbAsyncEnumerator<TType> GetAsyncEnumerator()
        {
            return new EFAsyncEnumerator<TType>(this.AsEnumerable().GetEnumerator());
        }

        /// <summary>
        /// Returns an IDbAsyncEnumerator which when enumerated will execute the query against the database.
        /// </summary>
        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return GetAsyncEnumerator();
        }
#endif
        #endregion

    }
}