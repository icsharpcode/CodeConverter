using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BenchmarkDotNet.Attributes;

namespace CodeConverter.Tests
{

    public static partial class AsyncParallelExtensions
    {

        public static IAsyncEnumerable<TResult> DataFlowParallelSelectAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            return DataFlowParallelSelectAsync(source, selector, maxDop, default);
        }

        private static async IAsyncEnumerable<TResult> DataFlowParallelSelectAsync<TArg, TResult>(IEnumerable<TArg> source, Func<TArg, Task<TResult>> selector, int maxDop, [EnumeratorCancellation] CancellationToken token)
        {
            var transform = new TransformBlock<TArg, TResult>(selector, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = maxDop,
                CancellationToken = token
            });

            var unused = source.Select(f => transform.Post(f)).ToArray();
            transform.Complete();

            await foreach (var item in transform.AsObservable().ToAsyncEnumerable().WithCancellation(token)) {
                yield return item;
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/58564740/1128762
        /// </summary>
        public static IAsyncEnumerable<TResult> DataFlowLazyParallelSelectAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            return DataFlowLazyParallelSelectAsync(source, selector, maxDop, default);
        }

        public static async IAsyncEnumerable<TResult> DataFlowLazyParallelSelectAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop, [EnumeratorCancellation] CancellationToken token)
        {
            var processor = new TransformBlock<TArg, TResult>(selector, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = maxDop,
                BoundedCapacity = (maxDop * 5) / 4,
                CancellationToken = token
            });

            foreach (var item in source) {
                while (!processor.Post(item)) {
                    yield return await WaitNextResultAsync();
                }
                if (processor.TryReceive(out var result)) {
                    yield return result;
                }
            }
            processor.Complete();

            while (await processor.OutputAvailableAsync(token)) {
                yield return Receive();
            }

            async Task<TResult> WaitNextResultAsync()
            {
                if (!await processor.OutputAvailableAsync()) throw new InvalidOperationException("No output available after posting output and waiting");
                return Receive();
            }

            TResult Receive()
            {
                if (!processor.TryReceive(out var result)) throw new InvalidOperationException("Nothing received even though output available");
                token.ThrowIfCancellationRequested();
                return result;
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/58564740/1128762
        /// </summary>
        public static async IAsyncEnumerable<TResult> DataFlowLazyParallelSelectAsync_PostThread<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            var processor = new TransformBlock<TArg, TResult>(selector, new ExecutionDataflowBlockOptions {
                MaxDegreeOfParallelism = maxDop
            });

            var postTask = Task.Run(() => {
                try {
                    foreach (var item in source) processor.Post(item);
                } finally {
                    processor.Complete();
                }
            });

            await foreach (var result in processor.AsObservable().ToAsyncEnumerable()) {
                yield return result;
            }

            await postTask; // Observe any exceptions from posting
        }

        public static async IAsyncEnumerable<TResult> AsParallelSelect<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            foreach (var task in source.AsParallel().AsOrdered().WithDegreeOfParallelism(maxDop).Select(selector).Select(r => r.Result)) {
                yield return task;
            }
        }

        public static IAsyncEnumerable<TResult> SingleThreadAsync<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            return source.ToAsyncEnumerable().SelectAwait(async x => await selector(x));
        }

        public static async Task<IEnumerable<int>> TaskPerItem_IgnoresMaxDop_NoValuesUntilEnd(this IEnumerable<int[]> inputs, Func<int[], Task<int>> work, int ignoredMaxDop)
        {
            return await Task.WhenAll(inputs.Select(work).ToArray());
        }

        public static IAsyncEnumerable<TResult> ExplicitPartition_Lazy<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            return Partitioner.Create(source).GetPartitions(maxDop).AsParallel()
                .Select(partition => AsAsyncEnumerable(partition).SelectAwait(async r => await selector(r)))
                .ToAsyncEnumerable().SelectMany(x => x);
        }

        public static async IAsyncEnumerable<TResult> ExplicitPartitions_Eager_NoValuesUntilEnd<TArg, TResult>(this IEnumerable<TArg> source,
            Func<TArg, Task<TResult>> selector, int maxDop)
        {
            var partitionTasks = Partitioner.Create(source).GetPartitions(maxDop)
                .AsParallel().WithDegreeOfParallelism(maxDop).Select(partition => SelectAsync(AsEnumerable(partition), selector));
            var partionedResults = await Task.WhenAll(partitionTasks);
            var results = partionedResults.SelectMany(x => x).ToAsyncEnumerable();
            await foreach (var result in results) yield return result;

            static async Task<TResult[]> SelectAsync(IEnumerable<TArg> source,
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

        private static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            using (enumerator) {
                while (enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
            }
        }

        private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerator<T> enumerator)
        {
            using (enumerator) {
                while (enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
            }
        }
    }
}
