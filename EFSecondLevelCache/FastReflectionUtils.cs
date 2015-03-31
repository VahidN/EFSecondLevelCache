using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace EFSecondLevelCache
{
    /// <summary>
    /// Fast reflection, using compiled property getters.
    /// </summary>
    public static class FastReflectionUtils
    {
        private static readonly
            ConcurrentDictionary<string, ConcurrentDictionary<Type, Func<object, object>>> _gettersCache
                = new ConcurrentDictionary<string, ConcurrentDictionary<Type, Func<object, object>>>();

        /// <summary>
        /// Gets a compiled property getter delegate for the underlying type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        public static Func<object, object> GetPropertyGetterDelegate(
            this Type type, string propertyName, BindingFlags bindingFlags)
        {
            var property = type.GetProperty(propertyName, bindingFlags);
            if (property == null)
                throw new InvalidOperationException(string.Format("Couldn't find the {0} property.", propertyName));

            var getMethod = property.GetGetMethod(nonPublic: true);
            if (getMethod == null)
                throw new InvalidOperationException(string.Format("Couldn't get the GetMethod of {0}", type));

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var getterExpression = Expression.Convert(
                Expression.Call(Expression.Convert(instanceParam, type), getMethod), typeof(object));
            return Expression.Lambda<Func<object, object>>(getterExpression, instanceParam).Compile();
        }

        /// <summary>
        /// Gets a compiled property getter delegate for the underlying type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        public static Func<object, object> GetPropertyGetterDelegateFromCache(
            this Type type, string propertyName, BindingFlags bindingFlags)
        {
            ConcurrentDictionary<Type, Func<object, object>> getterDictionary;
            Func<object, object> getter;
            if (_gettersCache.TryGetValue(propertyName, out getterDictionary))
            {
                if (getterDictionary.TryGetValue(type, out getter))
                {
                    return getter;
                }
            }

            getter = type.GetPropertyGetterDelegate(propertyName, bindingFlags);
            if (getter == null)
            {
                throw new NotSupportedException(string.Format("Failed to get {0}Getter.", propertyName));
            }

            if (getterDictionary != null)
            {
                getterDictionary.TryAdd(type, getter);
            }
            else
            {
                _gettersCache.TryAdd(propertyName,
                    new ConcurrentDictionary<Type, Func<object, object>>(
                        new Dictionary<Type, Func<object, object>>
                        {
                            { type, getter }
                        }));
            }

            return getter;
        }
    }
}