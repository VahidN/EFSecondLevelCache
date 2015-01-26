using System;
using EFSecondLevelCache.Contracts;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Computes the unique hash of the input, using the xxHash algorithm.
    /// </summary>
    public class EFCacheKeyHashProvider : IEFCacheKeyHashProvider
    {
        /// <summary>
        /// Computes the unique hash of the input.
        /// </summary>
        /// <param name="data">the input data to hash</param>
        /// <returns>Hashed data using the xxHash algorithm</returns>
        public string ComputeHash(string data)
        {
            if(string.IsNullOrWhiteSpace(data))
                throw new ArgumentNullException("data");

            return string.Format("{0:X}", XxHashUnsafe.ComputeHash(data));
        }
    }
}