using System;
using System.Web.Caching;

namespace EFSecondLevelCache.Contracts
{
    /// <summary>
    /// EFCachePolicy determines the AbsoluteExpiration time and Priority of the cache.
    /// </summary>
    public class EFCachePolicy
    {
        private DateTime? _absoluteExpiration;

        /// <summary>
        /// Its deafult value is 20 minutes later.
        /// </summary>
        public DateTime? AbsoluteExpiration
        {
            set { _absoluteExpiration = value; }
            get { return _absoluteExpiration ?? (_absoluteExpiration = DateTime.Now.AddMinutes(20)); }
        }

        /// <summary>
        /// Its deafult value is EF_.
        /// </summary>
        public string KeyHashPrefix { set; get; }

        /// <summary>
        /// If you think the computed hash of the query is not enough, set this value.
        /// Its deafult value is string.Empty.
        /// </summary>
        public string SaltKey { set; get; }

        /// <summary>
        /// Its deafult value is CacheItemPriority.Normal.
        /// </summary>
        public CacheItemPriority Priority { set; get; }

        /// <summary>
        /// EFCachePolicy determines the AbsoluteExpiration time and Priority of the cache.
        /// </summary>
        public EFCachePolicy()
        {
            KeyHashPrefix = EFCacheKey.KeyHashPrefix;
            SaltKey = string.Empty;
            Priority = CacheItemPriority.Normal;
        }
    }
}