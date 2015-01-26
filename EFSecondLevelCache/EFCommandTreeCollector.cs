using System;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure.Interception;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Receives notifications when Entity Framework creates a DbCommandTree.
    /// It collects only DbQueryCommandTrees.
    /// </summary>
    public class EFCommandTreeCollector : IDbCommandTreeInterceptor, IDisposable
    {
        private readonly ObjectContext _objectContext;

        /// <summary>
        /// Receives notifications when Entity Framework creates a DbCommandTree.
        /// </summary>
        /// <param name="objectContext">The associated objectContext of the collected DbQueryCommandTree.</param>
        public EFCommandTreeCollector(ObjectContext objectContext)
        {
            _objectContext = objectContext;
            DbInterception.Add(this);
        }

        /// <summary>
        /// The associated DbQueryCommandTree of the given ObjectContext.
        /// </summary>
        public DbQueryCommandTree DbQueryCommandTree { get; private set; }

        /// <summary>
        /// This method is called after a new DbCommandTree has been created.
        /// It collects only DbQueryCommandTrees.
        /// </summary>
        /// <param name="interceptionContext">Represents contextual information associated with calls into IDbCommandTreeInterceptor implementations.</param>
        public void TreeCreated(DbCommandTreeInterceptionContext interceptionContext)
        {
            if (interceptionContext.Result.CommandTreeKind == DbCommandTreeKind.Query
                && interceptionContext.Result.DataSpace == DataSpace.SSpace)
            {
                if (interceptionContext.ObjectContexts.Contains(_objectContext, ReferenceEquals))
                {
                    DbQueryCommandTree = (DbQueryCommandTree)interceptionContext.Result;
                }
            }
        }

        /// <summary>
        /// Removes the registered IDbInterceptor so that it will no longer receive notifications.
        /// </summary>
        public void Dispose()
        {
            DbInterception.Remove(this);
        }
    }
}