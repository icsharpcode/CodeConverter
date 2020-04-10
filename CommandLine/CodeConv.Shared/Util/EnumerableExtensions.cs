using System;
using System.Linq;
using System.Collections.Generic;

namespace ICSharpCode.CodeConverter.CommandLine.Util
{
    internal static class EnumerableExtensions
    {
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> lookup)
        {
            var duplicateProps = lookup.Where(p => p.Count() > 1).Select(p => p.Key).ToArray();
            if (duplicateProps.Any()) throw new ArgumentOutOfRangeException($"Duplicate keys for: {string.Join(", ", duplicateProps)}");
            return lookup.ToDictionary(p => p.Key, p => p.First());
        }

        public static bool TryAdd<TKey, TElement>(this IDictionary<TKey, TElement> lookup, TKey key, TElement element)
        {
            if (lookup.ContainsKey(key)) return false;
            lookup[key] = element;
            return true;
        }

        public static IEnumerable<T> Yield<T>(this T singleElement)
        {
            yield return singleElement;
        }
    }
}
