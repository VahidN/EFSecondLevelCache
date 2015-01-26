namespace EFSecondLevelCache.Contracts
{
    /// <summary>
    /// Stores information of the computed key of the input LINQ query.
    /// </summary>
    public class EFCacheKey
    {
        /// <summary>
        /// Its default value is EF_.
        /// </summary>
        public const string KeyHashPrefix = "EF_";

        /// <summary>
        /// The computed key of the input LINQ query.
        /// </summary>
        public string Key { set; get; }

        /// <summary>
        /// Hash of the input LINQ query's computed key.
        /// </summary>
        public string KeyHash { set; get; }

        /// <summary>
        /// Determines which entities are used in this LINQ query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </summary>
        public string[] CacheDependencies { set; get; }

        /// <summary>
        /// Stores information of the computed key of the input LINQ query.
        /// </summary>
        public EFCacheKey()
        {
            CacheDependencies = new[] { string.Empty };
        }
    }
}