using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EFSecondLevelCache.Contracts;
#if !NET40
using System.Data.Entity.Infrastructure;
#endif

namespace EFSecondLevelCache
{
    /// <summary>
    /// Defines methods to create and execute queries that are described by an System.Linq.IQueryable object.
    /// </summary>
    /// <typeparam name="TType">Type of the entity.</typeparam>
    public class EFCachedQueryProvider<TType> : IQueryProvider
#if !NET40
, IDbAsyncQueryProvider
#endif
    {
        private readonly IQueryable<TType> _query;
        private readonly EFCachePolicy _efCachePolicy;
        private readonly EFCacheDebugInfo _debugInfo;
        private readonly IEFCacheKeyProvider _cacheKeyProvider;
        private readonly IEFCacheServiceProvider _cacheServiceProvider;

        /// <summary>
        /// Defines methods to create and execute queries that are described by an System.Linq.IQueryable object.
        /// </summary>
        /// <param name="query">The input EF query.</param>
        /// <param name="efCachePolicy">Determines the AbsoluteExpiration time and Priority of the cache.</param>
        /// <param name="debugInfo">Stores the debug information of the caching process.</param>
        /// <param name="cacheKeyProvider">Gets an EF query and returns its hash to store in the cache.</param>
        /// <param name="cacheServiceProvider">The Cache Service Provider.</param>
        public EFCachedQueryProvider(
            IQueryable<TType> query,
            EFCachePolicy efCachePolicy,
            EFCacheDebugInfo debugInfo,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCacheServiceProvider cacheServiceProvider)
        {
            _query = query;
            _efCachePolicy = efCachePolicy;
            _debugInfo = debugInfo;
            _cacheKeyProvider = cacheKeyProvider;
            _cacheServiceProvider = cacheServiceProvider;
        }

        /// <summary>
        /// Constructs an System.Linq.IQueryable of T object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TElement">The type of the elements that is returned.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An System.Linq.IQueryable of T that can evaluate the query represented by the specified expression tree.</returns>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return _query.Provider.CreateQuery<TElement>(expression);
        }

        /// <summary>
        /// Constructs an System.Linq.IQueryable object that can evaluate the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>An System.Linq.IQueryable that can evaluate the query represented by the specified expression tree.</returns>
        public IQueryable CreateQuery(Expression expression)
        {
            return _query.Provider.CreateQuery(expression);
        }

        /// <summary>
        /// Executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Materialize(expression, () => _query.Provider.Execute(expression));
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public object Execute(Expression expression)
        {
            return Materialize(expression, () => _query.Provider.Execute(expression));
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree to cache its results.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="materializer">How to run the query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public object Materialize(Expression expression, Func<object> materializer)
        {
            var cacheKey = _cacheKeyProvider.GetEFCacheKey(
                                                            _query,
                                                            expression,
                                                            _efCachePolicy.KeyHashPrefix,
                                                            _efCachePolicy.SaltKey);
            _debugInfo.EFCacheKey = cacheKey;
            var queryCacheKey = cacheKey.KeyHash;
            var result = _cacheServiceProvider.GetValue(queryCacheKey);
            if(Equals(result, _cacheServiceProvider.NullObject))
            {
                _debugInfo.IsCacheHit = true;
                return null;
            }

            if (result != null)
            {
                _debugInfo.IsCacheHit = true;
                return result;
            }

            result = materializer();

            _cacheServiceProvider.StoreRootCacheKeys(cacheKey.CacheDependencies);
            if (_efCachePolicy.AbsoluteExpiration == null)
            {
                _efCachePolicy.AbsoluteExpiration = DateTime.Now.AddMinutes(20);
            }
            _cacheServiceProvider.InsertValue(
                queryCacheKey,
                result,
                cacheKey.CacheDependencies,
                _efCachePolicy.AbsoluteExpiration.Value,
                _efCachePolicy.Priority);

            return result;
        }

        #region IDbAsyncQueryProvider implementation
#if !NET40
        /// <summary>
        /// Asynchronously executes the strongly-typed query represented by a specified expression tree.
        /// </summary>
        /// <typeparam name="TResult">The type of the value that results from executing the query.</typeparam>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.  The task result contains the value that results from executing the specified query.</returns>
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute<TResult>(expression));
        }

        /// <summary>
        /// Asynchronously executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.  The task result contains the value that results from executing the specified query.</returns>
        public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(expression));
        }
#endif
        #endregion
    }
}