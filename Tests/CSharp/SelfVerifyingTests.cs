using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using Xunit;
using Xunit.Sdk;
using CodeConverter.Tests.Compilation;
using System.IO;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace CodeConverter.Tests.CSharp
{
    /// <summary>
    /// Run pairs of xUnit tests in converted code before and after conversion
    /// to verify code conversion did not break the tests.
    /// </summary>
    public class SelfVerifyingTests : ConverterTestBase
    {
        [Theory, MemberData(nameof(GetVisualBasicToCSharpTestData))]
        public void VisualBasicToCSharp(Action verifyConvertedTestPasses)
        {
            verifyConvertedTestPasses();
        }

        /// <summary>
        /// Compile VB.NET source, convert it to C#, compile the conversion
        /// and return actions which run each corresponding pair of tests.
        /// </summary>
        public static IEnumerable<object[]> GetVisualBasicToCSharpTestData()
        {
            var sourceFileText = File.ReadAllText("../../../TestData/SelfVerifyingTests/VBToCS/SelfVerifyingTest.vb");
            byte[] compiledSource = CompileSource<VBCompiler>(sourceFileText);

            var conversionResult = ProjectConversion.ConvertText<VBToCSConversion>(sourceFileText, DefaultReferences.NetStandard2);
            byte[] compiledTarget = CompileTarget<CSharpCompiler>(conversionResult.ConvertedCode);

            Dictionary<string, Action> runnableTestsInSource = TestDiscovery.GetTestNamesAndCallbacks(compiledSource);
            Dictionary<string, Action> runnableTestsInTarget = TestDiscovery.GetTestNamesAndCallbacks(compiledTarget);

            Assert.NotEmpty(runnableTestsInSource);
            Assert.Equal(runnableTestsInSource.Count, runnableTestsInTarget.Count);

            return runnableTestsInSource.Select(sourceTest =>
                new object[] {
                    (Action)(() => {
                        try {
                            sourceTest.Value();
                        } catch(TargetInvocationException ex) {
                            Fail($"Error running source version of test \"{sourceTest.Key}\": {(ex.InnerException ?? ex)}");
                        }
                        try {
                            runnableTestsInTarget[sourceTest.Key]();
                        } catch(TargetInvocationException ex) {
                            Fail($"Error running converted version of test \"{sourceTest.Key}\": {(ex.InnerException ?? ex)}");
                        }
                    })
                }
            );
        }

        private static byte[] CompileSource<TCompiler>(string sourceText) where TCompiler : ICompiler, new()
        {
            var compiler = new TCompiler();
            try {
                return compiler.Compile.FromString(sourceText, "SourceAssembly", AdditionalReferences);
            } catch (CompilationException ex) {
                throw new XunitException($"Error compiling source: {ex}");
            }
        }

        private static byte[] CompileTarget<TCompiler>(string targetText) where TCompiler : ICompiler, new()
        {
            var compiler = new TCompiler();
            try {
                return compiler.Compile.FromString(targetText, "TargetAssembly", AdditionalReferences);
            } catch (CompilationException ex) {
                throw new XunitException($"Error compiling target: {ex}");
            }
        }

        private static IEnumerable<MetadataReference> AdditionalReferences => new List<MetadataReference>()
        {
            MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(FactAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location), //this feels fragile and wrong but I'm not sure
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location)     //of a better way to reference these assemblies :(
        };
    }
}
