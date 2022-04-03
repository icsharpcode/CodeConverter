﻿using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.VB;
using ICSharpCode.CodeConverter.Shared;

namespace ICSharpCode.CodeConverter.Tests.VB;

/// <summary>
/// Run pairs of xUnit tests in converted code before and after conversion
/// to verify code conversion did not break the tests.
/// </summary>
public class SelfVerifyingTests
{
    [Theory, MemberData(nameof(GetCSharpToVisualBasicTestData))]
    public async Task VisualBasicToCSharpAsync(NamedTest verifyConvertedTestPasses)
    {
        await verifyConvertedTestPasses.Execute();
    }

    /// <summary>
    /// Compile VB.NET source, convert it to C#, compile the conversion
    /// and return actions which run each corresponding pair of tests.
    /// </summary>
    public static IEnumerable<object[]> GetCSharpToVisualBasicTestData()
    {
        var testFiles = Directory.GetFiles(Path.Combine(TestConstants.GetTestDataDirectory(), "SelfVerifyingTests/CSToVB"), "*.cs");
        return testFiles.SelectMany(SelfVerifyingTestFactory.GetSelfVerifyingFacts<CSharpCompiler, VisualBasicCompiler, CSToVBConversion>)
            .Select(et => new object[] {et})
            .ToArray();
    }
}