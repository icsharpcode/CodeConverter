using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared;

public class SingleConversionOptions : ConversionOptions
{
    public TextSpan SelectedTextSpan { get; set; }
    internal bool ShowCompilationErrors { get; init; }
}