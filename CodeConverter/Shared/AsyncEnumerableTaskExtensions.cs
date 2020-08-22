using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ICSharpCode.CodeConverter.Shared
{
    [DebuggerStepThrough]
    public static class AsyncEnumerableTaskExtensions
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
                CancellationToken = token,
                SingleProducerConstrained = true,
                EnsureOrdered = false
            });

            bool pipelineTerminatedEarly = false;

            foreach (var item in source) {
                while (!processor.Post(item)) {
                    var result = await ReceiveAsync();
                    if (pipelineTerminatedEarly) break;
                    yield return result;
                }
                if (pipelineTerminatedEarly) break;

                if (processor.TryReceive(out var resultIfAvailable)) {
                    yield return resultIfAvailable;
                }
            }
            processor.Complete();

            while (await processor.OutputAvailableAsync(token)) {
                var result = ReceiveKnownAvailable();
                if (pipelineTerminatedEarly) break;
                yield return result;
            }

            await processor.Completion;

            if (pipelineTerminatedEarly) {
                throw new InvalidOperationException("Pipeline terminated early missing items, but no exception thrown");
            }

            async Task<TResult> ReceiveAsync()
            {
                await processor.OutputAvailableAsync();
                return ReceiveKnownAvailable();
            }

            TResult ReceiveKnownAvailable()
            {
                token.ThrowIfCancellationRequested();
                if (!processor.TryReceive(out var item)) {
                    pipelineTerminatedEarly = true;
                    return default;
                }
                return item;
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