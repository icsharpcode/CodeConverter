using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ICSharpCode.CodeConverter.CommandLine.Util
{
    internal static class EnumerableExtensions
    {
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> lookup) where TKey: notnull
        {
            var duplicateProps = lookup.Where(p => p.Count() > 1).Select(p => p.Key).ToArray();
            if (duplicateProps.Any()) throw new ValidationException($"Duplicate keys for: {string.Join(", ", duplicateProps)}");
            return lookup.ToDictionary(p => p.Key, p => p.First());
        }

        public static bool TryAdd<TKey, TElement>(this IDictionary<TKey, TElement> dictionary, TKey key, TElement element) where TKey : notnull
        {
            if (dictionary.ContainsKey(key)) return false;
            dictionary[key] = element;
            return true;
        }

        public static IEnumerable<T> Yield<T>(this T singleElement)
        {
            yield return singleElement;
        }
    }
}
