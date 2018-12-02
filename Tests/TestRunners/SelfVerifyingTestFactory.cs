using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CodeConverter.Tests.Compilation;
using CodeConverter.Tests.CSharp;
using ICSharpCode.CodeConverter;
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
        public static IEnumerable<NamedFact> GetSelfVerifyingFacts<TSourceCompiler, TTargetCompiler, TLanguageConversion>(string testFilepath) 
            where TSourceCompiler : ICompiler, new() where TTargetCompiler : ICompiler, new() where TLanguageConversion : ILanguageConversion, new()
        {
            var sourceFileText = File.ReadAllText(testFilepath);
            byte[] compiledSource = CompileSource<TSourceCompiler>(sourceFileText);
            var runnableTestsInSource = XUnitFactDiscoverer.GetNamedFacts(compiledSource).ToList();
            return GetSelfVerifyingFacts<TTargetCompiler, TLanguageConversion>(sourceFileText, runnableTestsInSource);
        }

        private static IEnumerable<NamedFact> GetSelfVerifyingFacts<TTargetCompiler, TLanguageConversion>(string sourceFileText,
                List<NamedFact> runnableTestsInSource) where TTargetCompiler : ICompiler, new()
            where TLanguageConversion : ILanguageConversion, new()
        {
            ConversionResult conversionResult = ProjectConversion.ConvertText<TLanguageConversion>(sourceFileText, DefaultReferences.NetStandard2);

            // Avoid confusing test runner on error, but also avoid calculating multiple times
            var runnableTestsInTarget = new Lazy<Dictionary<string, NamedFact>>(() => GetConvertedNamedFacts<TTargetCompiler, TLanguageConversion>(runnableTestsInSource,
                conversionResult));

            return runnableTestsInSource.Select(sourceFact =>
                new NamedFact(sourceFact.Name, () =>
                {
                    try
                    {
                        sourceFact.Execute();
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new XunitException(
                            $"Error running source version of test \"{sourceFact.Name}\": {(ex.InnerException ?? ex)}");
                    }

                    try
                    {
                        runnableTestsInTarget.Value[sourceFact.Name].Execute();
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new XunitException(
                            $"Error running converted version of test \"{sourceFact.Name}\": {(ex.InnerException ?? ex)}\r\nConverted Code: {conversionResult.ConvertedCode ?? conversionResult.GetExceptionsAsString()}");
                    }
                })
            );
        }

        private static Dictionary<string, NamedFact> GetConvertedNamedFacts<TTargetCompiler, TLanguageConversion>(List<NamedFact> runnableTestsInSource, ConversionResult convertedText)
            where TTargetCompiler : ICompiler, new() where TLanguageConversion : ILanguageConversion, new()
        {
            byte[] compiledTarget = CompileTarget<TTargetCompiler>(convertedText.ConvertedCode);
            var runnableTestsInTarget = XUnitFactDiscoverer.GetNamedFacts(compiledTarget).ToDictionary(f => f.Name);

            Assert.NotEmpty(runnableTestsInSource);
            Assert.Equal(runnableTestsInSource.Select(f => f.Name), runnableTestsInTarget.Keys);
            return runnableTestsInTarget;
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