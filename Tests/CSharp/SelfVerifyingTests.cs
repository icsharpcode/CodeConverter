using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Common;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using Xunit;

namespace ICSharpCode.CodeConverter.Tests.CSharp;

/// <summary>
/// Run pairs of xUnit tests in converted code before and after conversion
/// to verify code conversion did not break the tests.
/// </summary>
public class SelfVerifyingTests
{
    //[Theory, MemberData(nameof(GetVisualBasicToCSharpTestData))]
    public async Task VisualBasicToCSharpAsync(NamedTest verifyConvertedTestPasses)
    {
        await verifyConvertedTestPasses.Execute();
    }

    /// <summary>
    /// Compile VB.NET source, convert it to C#, compile the conversion
    /// and return actions which run each corresponding pair of tests.
    /// </summary>
    public static IEnumerable<object[]> GetVisualBasicToCSharpTestData()
    {
        var testFiles = Directory.GetFiles(Path.Combine(TestConstants.GetTestDataDirectory(), "SelfVerifyingTests/VBToCS"), "*.vb");
        return testFiles.SelectMany(SelfVerifyingTestFactory.GetSelfVerifyingFacts<VisualBasicCompiler, CSharpCompiler, VBToCSConversion>)
            .Select(et => new object[] {et})
            .ToArray();
    }
}