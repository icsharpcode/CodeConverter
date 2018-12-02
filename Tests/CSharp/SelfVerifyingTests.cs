using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;
using CodeConverter.Tests.Compilation;
using System.IO;
using CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.Util;

namespace CodeConverter.Tests.CSharp
{
    /// <summary>
    /// Run pairs of xUnit tests in converted code before and after conversion
    /// to verify code conversion did not break the tests.
    /// </summary>
    public class SelfVerifyingTests
    {
        [Theory, MemberData(nameof(GetVisualBasicToCSharpTestData))]
        public void VisualBasicToCSharp(ExecutableTest verifyConvertedTestPasses)
        {
            verifyConvertedTestPasses.Execute();
        }

        /// <summary>
        /// Compile VB.NET source, convert it to C#, compile the conversion
        /// and return actions which run each corresponding pair of tests.
        /// </summary>
        public static IEnumerable<object[]> GetVisualBasicToCSharpTestData()
        {
            var testFiles = Directory.GetFiles("../../../TestData/SelfVerifyingTests/VBToCS", "*.vb");
            return testFiles.SelectMany(SelfVerifyingTestFactory.GetExecutableTests<VisualBasicCompiler, CSharpCompiler, VBToCSConversion>)
                .Select(et => new object[] {et});
        }
    }
}
