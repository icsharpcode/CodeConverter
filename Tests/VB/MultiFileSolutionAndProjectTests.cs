using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.VB;

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

    [Fact] /* enable for executing locally */
    public async Task ConvertWholeSolutionAsync()
    {
        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => true, Language.VB);
    }

    [Fact] /* enable for executing locally */
    public async Task ConvertCSharpConsoleAppOnlyAsync()
    {
        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name == "CSharpConsoleApp", Language.VB);
    }
}