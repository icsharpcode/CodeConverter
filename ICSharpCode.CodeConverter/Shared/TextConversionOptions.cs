using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Shared
{
    public class TextConversionOptions : SingleConversionOptions
    {
        private readonly ConversionOptions _conversionOptions;

        public TextConversionOptions(IReadOnlyCollection<PortableExecutableReference> references)
        {
            References = references;
            _conversionOptions = new ConversionOptions();
        }

        public IReadOnlyCollection<PortableExecutableReference> References { get; }
    }
}