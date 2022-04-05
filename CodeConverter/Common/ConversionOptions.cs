namespace ICSharpCode.CodeConverter.Common;

public class ConversionOptions
{
    public string RootNamespaceOverride { get; set; }
    public CompilationOptions TargetCompilationOptionsOverride { get; set; }
    public TimeSpan AbandonOptionalTasksAfter { get; set; } = TimeSpan.FromMinutes(30);
}