using System;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.LanguageAgnostic;

public class ParallelSelectAwaitTests
{
    private const int MaxDop = 8;
    private static readonly int[] Input = Enumerable.Range(1, MaxDop * 3).ToArray();

    [Fact]
    public async Task ExceptionDoesNotHaltPipelineAsync()
    {
        var asyncEnumerable = Input.ParallelSelectAwait(async i => {
            await Task.Delay(1);
            return i > 3 ? i : throw new ObjectDisposedException("Original");
        }, MaxDop);

        Assert.Throws<ObjectDisposedException>(() => asyncEnumerable.ToArrayAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public async Task ExceptionDoesNotHaltPipelineSyncAsync()
    {
        var asyncEnumerable = Input.ParallelSelectAwait(
            async i => i > 3 ? i : throw new ObjectDisposedException("Original")
            , MaxDop
        );

        Assert.Throws<ObjectDisposedException>(() => asyncEnumerable.ToArrayAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public async Task AllElementsProcessedAsync()
    {
        var array = await Input.ParallelSelectAwait(
            async i => i
            , MaxDop
        ).ToArrayAsync();

        Assert.Equal(Input.OrderBy(x => x), array.OrderBy(x => x));
    }

}