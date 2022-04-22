namespace ICSharpCode.CodeConverter.Common;

public class TextConversionOptions : SingleConversionOptions
{
    public TextConversionOptions(IReadOnlyCollection<PortableExecutableReference> references, string sourceFilePath = null)
    {
        References = references;
        SourceFilePath = sourceFilePath;
    }

    public IReadOnlyCollection<PortableExecutableReference> References { get; }
    public string SourceFilePath { get; }
}