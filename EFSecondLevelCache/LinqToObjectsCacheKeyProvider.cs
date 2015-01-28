using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EFSecondLevelCache.Contracts;

namespace EFSecondLevelCache
{
    /// <summary>
    /// A custom cache key provider for normal LINQ to objects queries, results of a Mocking process.
    /// </summary>
    public class LinqToObjectsCacheKeyProvider : IEFCacheKeyProvider
    {
        private readonly IEFCacheKeyHashProvider _cacheKeyHashProvider;

        /// <summary>
        /// A custom cache key provider for normal LINQ to objects queries.
        /// </summary>
        /// <param name="cacheKeyHashProvider">Provides the custom hashing algorithm.</param>
        public LinqToObjectsCacheKeyProvider(IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            _cacheKeyHashProvider = cacheKeyHashProvider;
        }

        /// <summary>
        /// Gets a LINQ to objects query and returns its hashed key to store in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <param name="query">The input query.</param>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="keyHashPrefix">Its default value is EF_.</param>
        /// <param name="saltKey">If you think the computed hash of the query is not enough, set this value.</param>
        /// <returns>Information of the computed key of the input LINQ query.</returns>
        public EFCacheKey GetEFCacheKey<T>(
            IQueryable<T> query,
            Expression expression,
            string keyHashPrefix = EFCacheKey.KeyHashPrefix,
            string saltKey = "")
        {
            var traceString = GetDebugView(expression);
            var key = string.Format("{0}{1}{2}", traceString, Environment.NewLine, saltKey);
            var keyHash = string.Format("{0}{1}", keyHashPrefix, _cacheKeyHashProvider.ComputeHash(key));
            return new EFCacheKey { Key = key, KeyHash = keyHash, CacheDependencies = new[] { typeof(T).FullName } };
        }

        /// <summary>
        /// Gets the string representation of an Expression.
        /// </summary>
        /// <param name="expression">the input expression</param>
        /// <returns>The string representation of the Expression</returns>
        public string GetDebugView(Expression expression)
        {
            var debugViewDelegate = typeof(Expression).GetPropertyGetterDelegateFromCache(
                "DebugView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var debugView = debugViewDelegate(expression);
            if (debugView == null)
            {
                throw new NotSupportedException("Failed to get DebugView.");
            }

            return (string)debugView;
        }
    }
}