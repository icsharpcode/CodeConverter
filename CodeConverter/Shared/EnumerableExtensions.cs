using System.Collections.ObjectModel;

namespace ICSharpCode.CodeConverter.Shared;

public static partial class EnumerableExtensions
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

    public static bool IsEmpty<T>(this IReadOnlyCollection<T> source)
    {
        return source.Count == 0;
    }

    public static T OnlyOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate = null)
    {
        if (predicate != null) source = source.Where(predicate);
        T previous = default(T);
        int count = 0;
        foreach (var element in source) {
            previous = element;
            if (++count > 1) return default(T);
        }
        return count == 1 ? previous : default(T);
    }

    public static T SelectFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate = null)
    {
        if (predicate != null) source = source.Where(predicate);
        T previous = default(T);
        int count = 0;
        foreach (var element in source) {
            previous = element;
            if (++count > 1) return default(T);
        }
        return count == 1 ? previous : default(T);
    }

    public static IEnumerable<T> Yield<T>(this T singleElement)
    {
        yield return singleElement;
    }

    public static async Task<IEnumerable<T>> YieldAsync<T>(this Task<T> task) => (await task).Yield();

    public static IEnumerable<T> YieldNotNull<T>(this T singleElement)
    {
        if (singleElement == null) yield break;
        else yield return singleElement;
    }

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items) {
            collection.Add(item);
        }
    }

    public static (T[] False, T[] True) SplitOn<T>(this IEnumerable<T> enumerable, Func<T, bool> groupSelector) => SplitOn(enumerable, groupSelector, x => x);
    public static (TOut[] False, TOut[] True) SplitOn<TIn, TOut>(this IEnumerable<TIn> enumerable, Func<TIn, bool> groupSelector, Func<TIn, TOut> elementSelector)
    {
        var lookup = enumerable.ToLookup(groupSelector, elementSelector);
        return (lookup[false].ToArray(), lookup[true].ToArray());
    }
}