using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.CodeConverter.Util
{
#if NR6
    public
#endif
    internal static partial class EnumerableExtensions
    {
        public static IEnumerable<T> Do<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }

            // perf optimization. try to not use enumerator if possible
            var list = source as IList<T>;
            if (list != null) {
                for (int i = 0, count = list.Count; i < count; i++) {
                    action(list[i]);
                }
            } else {
                foreach (var value in source) {
                    action(value);
                }
            }

            return source;
        }

        public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> source)
        {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            return new ReadOnlyCollection<T>(source.ToList());
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T value)
        {
            if (source == null) {
                throw new ArgumentNullException(nameof(source));
            }

            return source.ConcatWorker(value);
        }

        private static IEnumerable<T> ConcatWorker<T>(this IEnumerable<T> source, T value)
        {
            foreach (var v in source) {
                yield return v;
            }

            yield return value;
        }

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            var readOnlyCollection = source as IReadOnlyCollection<T>;
            if (readOnlyCollection != null) {
                return readOnlyCollection.Count == 0;
            }

            var genericCollection = source as ICollection<T>;
            if (genericCollection != null) {
                return genericCollection.Count == 0;
            }

            var collection = source as ICollection;
            if (collection != null) {
                return collection.Count == 0;
            }

            var str = source as string;
            if (str != null) {
                return str.Length == 0;
            }

            foreach (var t in source) {
                return false;
            }

            return true;
        }

        public static bool IsEmpty<T>(this IReadOnlyCollection<T> source)
        {
            return source.Count == 0;
        }

        private static readonly Func<object, bool> s_notNullTest = x => x != null;

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)
            where T : class
        {
            if (source == null) {
                return SpecializedCollections.EmptyEnumerable<T>();
            }

            return source.Where((Func<T, bool>)s_notNullTest);
        }

        public static IEnumerable<T> Yield<T>(this T singleElement)
        {
            yield return singleElement;
        }

        public static bool Contains<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            return sequence.Any(predicate);
        }
    }
}
