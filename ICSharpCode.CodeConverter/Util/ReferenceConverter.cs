using System.IO;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Util
{
    public class ReferenceConverter
    {
        public static MetadataReference ConvertReference(MetadataReference nonLanguageSpecificRef)
        {
            if (!(nonLanguageSpecificRef is CompilationReference cr)) return nonLanguageSpecificRef;

            using (var stream = new MemoryStream())
            {
                cr.Compilation.Emit(stream);
                return MetadataReference.CreateFromStream(stream);
            }
        }
    }
}