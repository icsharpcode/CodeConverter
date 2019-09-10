using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class AsyncEnumerableTaskExtensions {
        private const int DefaultMaxDop = 1;
        
        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            using (enumerator) {
                while (enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
            }
        }

        public static async Task<TResult[]> SelectManyAsync<TArg, TResult>(this IEnumerable<TArg> nodes, Func<TArg, Task<IEnumerable<TResult>>> selector, byte maxDop = DefaultMaxDop)
        {
            var selectAsync = await nodes.SelectAsync(selector);
            return selectAsync.SelectMany(x => x).ToArray();
        }

        public static async Task<TResult[]> SelectAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, byte maxDop = DefaultMaxDop, bool maintainOrder = true)
        {
            if (maxDop <= 0) {
                throw new ArgumentOutOfRangeException(nameof(maxDop), maxDop, null);
            }

            return maintainOrder
                ? await SelectAsync(source, (a, i) => selector(a), maxDop, true)
                : await SelectAsyncInner(source, selector, maxDop);
        }

        public static async Task<TResult[]> SelectAsync<TArg, TResult>(this IEnumerable<TArg> nodes, Func<TArg, int, Task<TResult>> selector, byte maxDop = DefaultMaxDop, bool maintainOrder = true)
        {
            var nodesWithOrders = nodes.Select((input, originalOrder) => (input, originalOrder));

            IEnumerable<(TResult Result, int OriginalOrder)> resultsAndOrders = await nodesWithOrders
                .SelectAsyncInner(async arg => (Result: await selector(arg.input, arg.originalOrder), arg.originalOrder), maxDop);

            if (maintainOrder) {
                resultsAndOrders = resultsAndOrders.OrderBy(r => r.OriginalOrder);
            }
            return resultsAndOrders.Select(n => n.Result).ToArray();
        }

        private static async Task<TResult[]> SelectAsyncInner<TArg, TResult>(this IEnumerable<TArg> source, Func<TArg, Task<TResult>> selector, byte maxDop)
        {
            var partitionTasks = Partitioner.Create(source).GetPartitions(maxDop)
                .AsParallel().Select(partition => partition.AsEnumerable().SerialSelectAsync(selector));
            var partionedResults = await Task.WhenAll(partitionTasks);
            return partionedResults.SelectMany(x => x).ToArray();
        }

        private static async Task<List<TResult>> SerialSelectAsync<TArg, TResult>(this IEnumerable<TArg> enumerable, Func<TArg, Task<TResult>> selector)
        {
            var partitionResults = new List<TResult>();
            foreach (var partitionMember in enumerable) {
                var result = await selector(partitionMember);
                partitionResults.Add(result);
            }

            return partitionResults;
        }
    }
}