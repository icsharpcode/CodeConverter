using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

/// <summary>
/// <see cref="MultiFileTestFixture"/> for info on how these tests work.
/// </summary>
[Collection(MultiFileTestFixture.Collection)]
public class MultiFileSolutionAndProjectTests
{
    private readonly MultiFileTestFixture _multiFileTestFixture;

    public MultiFileSolutionAndProjectTests(MultiFileTestFixture multiFileTestFixture)
    {
        _multiFileTestFixture = multiFileTestFixture;
    }

    [Fact]
    public async Task ConvertWholeSolutionAsync()
    {

        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => true, Language.CS);
    }

    [Fact]
    public async Task ConvertVbLibraryOnlyAsync()
    {
        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name == "VbLibrary", Language.CS);
    }

    [Fact(Skip= "Roslyn bugs mean we can't run this test: https://github.com/icsharpcode/CodeConverter/pull/1116#issuecomment-2242645546")]
    public async Task ConvertVbUsingCSharpRefReturnOnlyAsync()
    {
        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name == "VisualBasicUsingCSharpRefReturn", Language.CS);
    }
}