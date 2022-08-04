using ICSharpCode.CodeConverter.CSharp;
using ICSharpCode.CodeConverter.VB;

namespace ICSharpCode.CodeConverter;

public static class CodeConverter
{
    private const string VbLanguage = "Visual Basic";
    private const string CsLanguage = "C#";

    private record SupportedConversion(string From, string To, Func<CodeWithOptions, CancellationToken, Task<ConversionResult>> ConvertAsync)
    {
        public override string ToString() => $"{From}->{To}";
    }

    private static readonly SupportedConversion[] SupportedConversions = {
        new(VbLanguage, CsLanguage, ConvertFromCodeWithOptionsAsync<VBToCSConversion>),
        new(CsLanguage, VbLanguage, ConvertFromCodeWithOptionsAsync<CSToVBConversion>)
    };

    private static Task<ConversionResult> ConvertFromCodeWithOptionsAsync<TLanguageConversion>(CodeWithOptions code, CancellationToken ct) where TLanguageConversion : ILanguageConversion, new() =>
        ProjectConversion.ConvertTextAsync<TLanguageConversion>(code.Text, new TextConversionOptions(code.References), cancellationToken: ct);

    public static async Task<ConversionResult> ConvertAsync(CodeWithOptions code, CancellationToken ct = default)
    {
        if (SupportedConversions.SingleOrDefault(c => c.From == code.FromLanguage && c.To == code.ToLanguage) is { } supportedConversion) {
            return await supportedConversion.ConvertAsync(code, ct);
        }

        string supportedConversions = string.Join<SupportedConversion>(", ", SupportedConversions);
        var exception = new NotSupportedException($"Conversion {code.FromLanguage} to {code.ToLanguage} is not supported, please specify one of the following: {supportedConversions}");
        return new ConversionResult(exception);
    }
}