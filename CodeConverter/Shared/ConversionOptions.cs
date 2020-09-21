using System;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public class ConversionOptions
    {
        public string RootNamespaceOverride { get; set; }
        public object LanguageVersionOverride { get; set; }
        public CompilationOptions TargetCompilationOptionsOverride { get; set; }
        public TimeSpan AbandonOptionalTasksAfter { get; set; } = TimeSpan.FromMinutes(30);
    }
}