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

    [Fact()]
    public async Task ConvertWholeSolutionAsync()
    {
        //the `CSharpRefReturn` project is excluded because it has ref return properties which are not supported in VB
        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name != "CSharpRefReturn", Language.VB);
    }

    [Fact()]
    public async Task ConvertCSharpConsoleAppOnlyAsync()
    {
        await _multiFileTestFixture.ConvertProjectsWhereAsync(p => p.Name == "CSharpConsoleApp", Language.VB);
    }
}