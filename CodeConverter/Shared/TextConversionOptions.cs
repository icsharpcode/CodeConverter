using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public class TextConversionOptions : SingleConversionOptions
    {
        private readonly ConversionOptions _conversionOptions;

        public TextConversionOptions(IReadOnlyCollection<PortableExecutableReference> references, string sourceFilePath = null)
        {
            References = references;
            SourceFilePath = sourceFilePath;
            _conversionOptions = new ConversionOptions();
        }

        public IReadOnlyCollection<PortableExecutableReference> References { get; }
        public string SourceFilePath { get; }
    }
}