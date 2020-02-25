using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ICSharpCode.CodeConverter.Shared
{
    internal static class AsyncEnumerableTaskExtensions
    {
        public static async Task<TResult[]> SelectManyAsync<TArg, TResult>(this IEnumerable<TArg> nodes,
            Func<TArg, Task<IEnumerable<TResult>>> selector)
        {
            var selectAsync = await nodes.SelectAsync(selector);
            return selectAsync.SelectMany(x => x).ToArray();
        }

        public static async IAsyncEnumerable<TResult> ParallelSelectAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, byte maxDop)
        {
            foreach (var item in source.AsParallel().WithDegreeOfParallelism(maxDop)) {
                yield return await selector(item);
            }
        }

        public static async Task<TResult[]> SelectAsync<TArg, TResult>(this IEnumerable<TArg> nodes,
            Func<TArg, int, Task<TResult>> selector)
        {
            var nodesWithOrders = nodes.Select((input, originalOrder) => (input, originalOrder));
            return await nodesWithOrders.SelectAsync(nwo => selector(nwo.input, nwo.originalOrder));
        }

        public static async Task<TResult[]> SelectAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector)
        {
            var partitionResults = new List<TResult>();
            foreach (var partitionMember in source) {
                var result = await selector(partitionMember);
                partitionResults.Add(result);
            }

            return partitionResults.ToArray();
        }
    }
}