using System;
using System.Collections.Generic;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Defines methods to support the comparison of objects for equality.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DynamicEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private readonly Func<T, T, bool> _func;

        /// <summary>
        /// Defines methods to support the comparison of objects for equality.
        /// </summary>
        /// <param name="func">The comparison algorithm.</param>
        public DynamicEqualityComparer(Func<T, T, bool> func)
        {
            _func = func;
        }

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type T to compare.</param>
        /// <param name="y">The second object of type T to compare.</param>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(T x, T y)
        {
            return _func(x, y);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="obj">The System.Object for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified object.</returns>
        public int GetHashCode(T obj)
        {
            return 0; // force Equals
        }
    }
}