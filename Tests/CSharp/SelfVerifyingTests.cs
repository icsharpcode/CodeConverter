using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using Xunit;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Tests.TestRunners;
using ICSharpCode.CodeConverter.Shared;

namespace ICSharpCode.CodeConverter.Tests.CSharp
{
    /// <summary>
    /// Run pairs of xUnit tests in converted code before and after conversion
    /// to verify code conversion did not break the tests.
    /// </summary>
    public class SelfVerifyingTests
    {
        [Theory, MemberData(nameof(GetVisualBasicToCSharpTestData))]
        public async Task VisualBasicToCSharpAsync(NamedFact verifyConvertedTestPasses)
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
}
