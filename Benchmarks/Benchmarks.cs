using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CodeConverter.Tests
{

    //[SimpleJob(RuntimeMoniker.CoreRt31, warmupCount: 1, targetCount: 2)]
    [SimpleJob(RuntimeMoniker.Net472, warmupCount: 1, targetCount: 5)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class Benchmarks
    {
        private static IConfig _config =
#if DEBUG
            new DebugInProcessConfig();
#else
            DefaultConfig.Instance;
#endif

        public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Benchmarks).Assembly).Run(args, _config);

        private int[][] _inputs = null;

        [Params(8, 16)]
        public int MaxDop { get; set; }

        [Params(80)]
        public byte PiecesOfWork { get; set; }

        [Params(500)]
        public int MinWorkSize { get; set; }

        [Params(10000)]
        public int MaxWorkSize { get; set; }

        [Params(4378)]
        public int RandomSeed { get; set; }

        /// <summary>
        /// The idea is to ensure low latency for the first result.
        /// A successful algorith must take less than half the time taking 1 as taking all.
        /// A cancellation token isn't currently part of the test, so extra work may be ongoing after the first one is returned and stop the benchmark process exiting.
        /// </summary>
        [Params(1, byte.MaxValue)]
        public byte TakeN { get; set; }

        private int? ExpectedAnswer;


        [GlobalSetup]
        public void GlobalSetup()
        {
            var rnd = new Random(RandomSeed);
            _inputs = Enumerable.Range(1, PiecesOfWork).Select(_ =>
                Enumerable.Range(1, rnd.Next(MinWorkSize, MaxWorkSize)).Select(__ => rnd.Next(1000)).ToArray()
            ).ToArray();
            ExpectedAnswer = SingleThread().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public Task<int> SingleThread()
        {
            return Run(AsyncParallelExtensions.SingleThreadAsync);
        }

        [Benchmark, BenchmarkCategory("GoodThroughput")]
        public Task<int> AsParallelSelect()
        {
            return Run(AsyncParallelExtensions.AsParallelSelect);
        }

        [Benchmark, BenchmarkCategory("GoodLatency")]
        public Task<int> ExplicitPartition_Lazy()
        {
            return Run(AsyncParallelExtensions.ExplicitPartition_Lazy);
        }

        [Benchmark, BenchmarkCategory("Candidate", "GoodLatency", "GoodThroughput")]
        public Task<int> DataFlowLazyParallelSelectAsync()
        {
            return Run(AsyncParallelExtensions.DataFlowLazyParallelSelectAsync);
        }

        [Benchmark, BenchmarkCategory("Variant", "GoodThroughput")]
        public Task<int> DataFlowParallelSelectAsync()
        {
            return Run(AsyncParallelExtensions.DataFlowParallelSelectAsync);
        }

        [Benchmark, BenchmarkCategory("Variant")]
        public Task<int> DataFlowLazyParallelSelectAsync_PostThread()
        {
            return Run(AsyncParallelExtensions.DataFlowLazyParallelSelectAsync_PostThread);
        }

        [Benchmark, BenchmarkCategory("NoValuesUntilEnd")]
        public Task<int> ExplicitPartitions_Eager_NoValuesUntilEnd()
        {
            return Run(AsyncParallelExtensions.ExplicitPartitions_Eager_NoValuesUntilEnd);
        }

        private async Task<int> Run(Func<IEnumerable<int[]>, Func<int[], Task<int>>, int, IAsyncEnumerable<int>> method)
        {
            await new SynchronizationContextRemover();
            var array = method(_inputs, DoDeterministicVariableCpuBoundWork, MaxDop);
            int max = await array.Take(TakeN).MaxAsync();
            return ThrowIfIncorrect(max);
        }

        private int ThrowIfIncorrect(int max)
        {
            return !ExpectedAnswer.HasValue || max == ExpectedAnswer ? max : throw new Exception($"Expected {ExpectedAnswer} but got {max}");
        }

        private async Task<int> DoDeterministicVariableCpuBoundWork(int[] input)
        {
            unchecked {
                int output = 1;
                for (int i = 0; i < input.Length; i++) {
                    for (int j = 0; j < input.Length; j++) {
                        output += 31 * input[i] - 11 * input[j];
                    }
                    if (i == input.Length / 2) await Task.Delay(1); //Simulate the task being half synchronous
                }
                return output;
            }
        }
    }
}
