using System;
using System.Linq;
using System.Collections.Generic;

namespace ICSharpCode.CodeConverter.DotNetTool
{
    internal static class EnumerableExtensions
    {
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(this IEnumerable<IGrouping<TKey, TElement>> lookup)
        {
            var duplicateProps = lookup.Where(p => p.Count() > 1).Select(p => p.Key).ToArray();
            if (duplicateProps.Any()) throw new ArgumentOutOfRangeException($"Duplicate keys for: {string.Join(", ", duplicateProps)}");
            return lookup.ToDictionary(p => p.Key, p => p.First());
        }
    }
}
