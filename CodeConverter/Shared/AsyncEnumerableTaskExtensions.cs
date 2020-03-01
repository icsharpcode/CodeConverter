using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

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

        /// <summary>High throughput parallel lazy-ish method</summary>
        /// <remarks>
        /// Inspired by https://stackoverflow.com/a/58564740/1128762
        /// </remarks>
        public static async IAsyncEnumerable<TResult> ParallelSelectAwait<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop, [EnumeratorCancellation] CancellationToken token = default)
        {
            var processor = new TransformBlock<TArg, TResult>(selector, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = maxDop,
                BoundedCapacity = (maxDop * 5) / 4,
                CancellationToken = token
            });

            foreach (var item in source) {
                while (!processor.Post(item)) {
                    yield return await ReceiveAsync();
                }
                if (processor.TryReceive(out var result)) {
                    yield return result;
                }
            }
            processor.Complete();

            while (await processor.OutputAvailableAsync(token)) {
                yield return Receive();
            }

            async Task<TResult> ReceiveAsync()
            {
                if (!await processor.OutputAvailableAsync() && !token.IsCancellationRequested) throw new InvalidOperationException("No output available after posting output and waiting");
                return Receive();
            }

            TResult Receive()
            {
                token.ThrowIfCancellationRequested();
                if (!processor.TryReceive(out var result)) throw new InvalidOperationException("Nothing received even though output available");
                return result;
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