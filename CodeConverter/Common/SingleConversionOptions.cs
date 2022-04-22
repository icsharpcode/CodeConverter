using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Common;

public class SingleConversionOptions : ConversionOptions
{
    public TextSpan SelectedTextSpan { get; set; }
    internal bool ShowCompilationErrors { get; init; }
}