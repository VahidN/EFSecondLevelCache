using System;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Extension methods for ObjectQuery.
    /// </summary>
    public static class ObjectQueryExtensions
    {
        private const BindingFlags PrivateMembersFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private static MethodInfo _createQueryMethod;
        static ObjectQueryExtensions()
        {
            cacheCreateQueryMethod();
        }

        private static void cacheCreateQueryMethod()
        {
            var objectQueryProviderType = typeof(DefaultExpressionVisitor).Assembly
                .GetType("System.Data.Entity.Core.Objects.ELinq.ObjectQueryProvider");
            if (objectQueryProviderType == null)
            {
                throw new NotSupportedException("Failed to get ObjectQueryProvider.");
            }

            _createQueryMethod = objectQueryProviderType.GetMethod("CreateQuery",
                PrivateMembersFlags,
                Type.DefaultBinder,
                new[] { typeof(Expression), typeof(Type) },
                null);
            if (_createQueryMethod == null)
            {
                throw new NotSupportedException("Failed to get CreateQuery Method.");
            }
        }

        /// <summary>
        /// Converts the query into an ObjectQuery.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="query">The query to convert.</param>
        /// <returns>The converted ObjectQuery</returns>
        public static ObjectQuery<TEntity> GetObjectQuery<TEntity>(this IQueryable<TEntity> query)
        {
            var originalObjectQuery = query as ObjectQuery<TEntity>;
            if (originalObjectQuery != null)
                return originalObjectQuery;

            var dbQuery = query as DbQuery<TEntity>;
            if (dbQuery == null)
            {
                throw new NotSupportedException(
                    "Failed to get DbQuery. Please use EFSecondLevelCache library with EntityFramework queries.");
            }

            var queryType = query.GetType();
            var internalQueryDelegate = queryType.GetPropertyGetterDelegateFromCache("InternalQuery", PrivateMembersFlags);
            var internalQuery = internalQueryDelegate(query);
            if (internalQuery == null)
            {
                throw new NotSupportedException("Failed to get InternalQuery.");
            }

            var internalQueryType = internalQuery.GetType();
            var objectQueryDelegate = internalQueryType.GetPropertyGetterDelegateFromCache("ObjectQuery", PrivateMembersFlags);
            var objectQuery = objectQueryDelegate(internalQuery) as ObjectQuery<TEntity>;
            if (objectQuery == null)
            {
                throw new NotSupportedException("Failed to get ObjectQuery.");
            }

            return objectQuery;
        }


        /// <summary>
        /// Creates an ObjectQuery from an expression.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="query">The input query.</param>
        /// <param name="expression">The input expression.</param>
        /// <returns>An ObjectQuery created from the expression.</returns>
        public static ObjectQuery<TEntity> GetObjectQuery<TEntity>(this IQueryable<TEntity> query, Expression expression)
        {
            var sourceQuery = query.GetObjectQuery();
            if (sourceQuery == null)
            {
                throw new NotSupportedException("Failed to get ObjectQuery.");
            }

            var provider = ((IQueryable<TEntity>)sourceQuery).Provider;
            var expressionQuery = _createQueryMethod.Invoke(
                        provider, new object[] { expression, typeof(TEntity) }) as IQueryable<TEntity>;
            if (expressionQuery == null)
            {
                throw new NotSupportedException("Failed to get expressionQuery.");
            }

            return expressionQuery.GetObjectQuery();
        }
    }
}