using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Objects;
using System.Linq;
using EFSecondLevelCache.Contracts;
using System.Linq.Expressions;

namespace EFSecondLevelCache
{
    /// <summary>
    /// A custom cache key provider for EF queries.
    /// </summary>
    public class EFCacheKeyProvider : IEFCacheKeyProvider
    {
        private readonly IEFCacheKeyHashProvider _cacheKeyHashProvider;
        private static readonly ConcurrentDictionary<string, string[]> _entityTypesCache = new ConcurrentDictionary<string, string[]>();

        /// <summary>
        /// A custom cache key provider for EF queries.
        /// </summary>
        /// <param name="cacheKeyHashProvider">Provides the custom hashing algorithm.</param>
        public EFCacheKeyProvider(IEFCacheKeyHashProvider cacheKeyHashProvider)
        {
            _cacheKeyHashProvider = cacheKeyHashProvider;
        }

        /// <summary>
        /// Gets an EF query and returns its hashed key to store in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the entity</typeparam>
        /// <param name="query">The EF query.</param>
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
            var objectQuery = query.GetObjectQuery(expression);
            objectQuery.MergeOption = MergeOption.NoTracking; // equals to call AsNoTracking() method

            string traceString;
            DbQueryCommandTree dbQueryCommandTree;
            using (var commandTreeCollector = new EFCommandTreeCollector(objectQuery.Context))
            {
                traceString = objectQuery.ToTraceString();
                dbQueryCommandTree = commandTreeCollector.DbQueryCommandTree;
            }

            var traceStringHash = _cacheKeyHashProvider.ComputeHash(traceString);
            var key = string.Format("{0}{3}{1}{3}{2}{3}{4}",
                                    objectQuery.Context.Connection.ConnectionString,
                                    traceString,
                                    string.Join(Environment.NewLine, getParameterValues(objectQuery)),
                                    Environment.NewLine,
                                    saltKey);
            var keyHash = string.Format("{0}{1}", keyHashPrefix, _cacheKeyHashProvider.ComputeHash(key));

            string[] cacheDependencies;
            if (_entityTypesCache.TryGetValue(traceStringHash, out cacheDependencies))
            {
                return new EFCacheKey { Key = key, KeyHash = keyHash, CacheDependencies = cacheDependencies };
            }

            cacheDependencies = getCacheDependencies(objectQuery, dbQueryCommandTree);
            _entityTypesCache.TryAdd(traceStringHash, cacheDependencies);

            return new EFCacheKey { Key = key, KeyHash = keyHash, CacheDependencies = cacheDependencies };
        }

        private static string[] getCacheDependencies(ObjectQuery objectQuery, DbQueryCommandTree queryTree)
        {
            if (queryTree == null)
                throw new KeyNotFoundException("Couldn't find the related DbCommandTree.");

            var visitor = new EFCommandTreeVisitor(objectQuery.Context.MetadataWorkspace);
            queryTree.Query.Accept(visitor);
            return visitor.EntityClrTypes.Select(x => x.FullName).ToArray();
        }

        private static IEnumerable<string> getParameterValues(ObjectQuery query)
        {
            return query.Parameters.Select(p => string.Format("{0}={1}", p.Name, p.Value));
        }
    }
}