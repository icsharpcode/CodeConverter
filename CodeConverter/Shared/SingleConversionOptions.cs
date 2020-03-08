using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.CodeConverter.Shared
{
    public class SingleConversionOptions : ConversionOptions
    {
        public TextSpan SelectedTextSpan { get; set; } = new TextSpan();
        internal bool ShowCompilationErrors { get; set; }
    }
}