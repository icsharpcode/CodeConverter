using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CodeConverter.Tests.Compilation;
using CodeConverter.Tests.CSharp;
using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.Shared;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Sdk;

namespace CodeConverter.Tests.TestRunners
{
    internal class SelfVerifyingTestFactory
    {
        public static IEnumerable<ExecutableTest> GetExecutableTests<TSourceCompiler, TTargetCompiler, TLanguageConversion>(string testFilepath) 
            where TSourceCompiler : ICompiler, new() where TTargetCompiler : ICompiler, new() where TLanguageConversion : ILanguageConversion, new()
        {
            var sourceFileText = File.ReadAllText(testFilepath);
            byte[] compiledSource = CompileSource<TSourceCompiler>(sourceFileText);

            var conversionResult =
                ProjectConversion.ConvertText<TLanguageConversion>(sourceFileText, DefaultReferences.NetStandard2);
            byte[] compiledTarget = CompileTarget<TTargetCompiler>(conversionResult.ConvertedCode);

            Dictionary<string, Action> runnableTestsInSource = TestDiscovery.GetTestNamesAndCallbacks(compiledSource);
            Dictionary<string, Action> runnableTestsInTarget = TestDiscovery.GetTestNamesAndCallbacks(compiledTarget);

            Assert.NotEmpty(runnableTestsInSource);
            Assert.Equal(runnableTestsInSource.Keys, runnableTestsInTarget.Keys);

            return runnableTestsInSource.Select(sourceTest =>
                new ExecutableTest(sourceTest.Key, () =>
                {
                    try
                    {
                        sourceTest.Value();
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new XunitException(
                            $"Error running source version of test \"{sourceTest.Key}\": {(ex.InnerException ?? ex)}");
                    }

                    try
                    {
                        runnableTestsInTarget[sourceTest.Key]();
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new XunitException(
                            $"Error running converted version of test \"{sourceTest.Key}\": {(ex.InnerException ?? ex)}");
                    }
                })
            );
        }

        private static byte[] CompileSource<TCompiler>(string sourceText) where TCompiler : ICompiler, new()
        {
            try {
                return new CompilerFrontend(new TCompiler()).FromString(sourceText, AdditionalReferences);
            } catch (CompilationException ex) {
                throw new XunitException($"Error compiling source: {ex}");
            }
        }

        private static byte[] CompileTarget<TCompiler>(string targetText) where TCompiler : ICompiler, new()
        {
            try {
                return new CompilerFrontend(new TCompiler()).FromString(targetText, AdditionalReferences);
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