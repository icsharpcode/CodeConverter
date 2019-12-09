using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public class ConversionOptions
    {
        public string RootNamespaceOverride { get; set; }
        public CompilationOptions TargetCompilationOptionsOverride { get; set; }
    }
}