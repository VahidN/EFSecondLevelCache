using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Determines the cache dependencies of a given query.
    /// </summary>
    public class EFCommandTreeVisitor : BasicCommandTreeVisitor
    {
        private static readonly ConcurrentDictionary<EntitySetBase, Type> _entityTypesCache = new ConcurrentDictionary<EntitySetBase, Type>();
        private readonly List<Type> _entityClrTypes = new List<Type>();
        private readonly MetadataWorkspace _metadata;

        /// <summary>
        /// Determines the cache dependencies of a given query.
        /// </summary>
        /// <param name="metadata">Runtime Metadata Workspace</param>
        public EFCommandTreeVisitor(MetadataWorkspace metadata)
        {
            _metadata = metadata;
        }

        /// <summary>
        /// Returns the cache dependencies of a given query.
        /// </summary>
        public IEnumerable<Type> EntityClrTypes
        {
            get { return _entityClrTypes; }
        }

        /// <summary>
        /// Implements the visitor pattern for the command tree.
        /// </summary>
        /// <param name="expression">Represents a scan of all elements of a given entity set.</param>
        public override void Visit(DbScanExpression expression)
        {
            var type = getEntityType(expression.Target);
            if (type != null) _entityClrTypes.Add(type);
            base.Visit(expression);
        }

        /// <summary>
        /// Finds if the table is an entity framework specific table.
        /// </summary>
        /// <param name="setBase">The set base</param>
        /// <returns>True if it's an EF internal table</returns>
        private static bool isEntityFrameworkInternalTable(EntitySetBase setBase)
        {
            return setBase.Table.StartsWith("__") || setBase.Table.StartsWith("Edm");
        }

        private Type getEntityType(EntitySetBase setBase)
        {
            Type setBaseType;
            if (_entityTypesCache.TryGetValue(setBase, out setBaseType))
            {
                return setBaseType;
            }

            // if it's an entity framework internal table then return null
            if (isEntityFrameworkInternalTable(setBase))
            {
                return null;
            }

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)_metadata.GetItemCollection(DataSpace.OSpace));

            // Get conceptual model
            var primitiveTypeCollection = _metadata.GetItems<EntityContainer>(DataSpace.CSpace).Single();

            // Get the mapping model
            var entityPrimitiveMappingCollection =
                _metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace).Single();

            // Get the entity type from the model and find which entities this set base refers to
            var oSpace = _metadata.GetItems<EntityType>(DataSpace.OSpace);
            foreach (var entityType in oSpace)
            {
                // Get the entity set that uses this entity type
                var entitySet = primitiveTypeCollection.EntitySets
                    .SingleOrDefault(s => s.ElementType.Name == entityType.Name);

                if (entitySet == null)
                {
                    continue;
                }

                // Find the mapping between conceptual and storage model for this entity set
                var mapping = entityPrimitiveMappingCollection.EntitySetMappings
                    .Single(s => s.EntitySet == entitySet);

                // Find the storage entity set (table) that the entity is mapped to.
                // This could be mapped to multiple entities
                var isRelatedTable = mapping.EntityTypeMappings
                    .SelectMany(typeMapping => typeMapping.Fragments)
                    .Select(fragment => fragment.StoreEntitySet)
                    .Select(set => set.MetadataProperties)
                    .Any(metadataCollection => (string)metadataCollection["Table"].Value == setBase.Table);

                // is this the table we are looking for?
                if (isRelatedTable)
                {
                    var clrType = objectItemCollection.GetClrType(entityType);
                    _entityTypesCache.TryAdd(setBase, clrType);
                    return clrType;
                }
            }

            // not found!
            return null;
        }
    }
}