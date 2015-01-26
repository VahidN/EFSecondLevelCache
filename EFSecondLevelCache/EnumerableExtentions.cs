using System;
using System.Collections.Generic;
using System.Linq;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Enumerable Extentions.
    /// </summary>
    public static class EnumerableExtentions
    {
        /// <summary>
        /// Determines whether a sequence contains a specified element by using the DynamicEqualityComparer of T.
        /// </summary>
        /// <typeparam name="T"> The type of the elements of source.</typeparam>
        /// <param name="source">A sequence in which to locate a value.</param>
        /// <param name="value">The value to locate in the sequence.</param>
        /// <param name="func">The comparison algorithm.</param>
        /// <returns>true if the source sequence contains an element that has the specified value; otherwise, false.</returns>
        public static bool Contains<T>(this IEnumerable<T> source, T value, Func<T, T, bool> func) where T : class
        {
            return source.Contains(value, new DynamicEqualityComparer<T>(func));
        }
    }
}