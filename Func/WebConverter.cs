using System;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Common;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Web;

public record ConvertRequest(string code, string requestedConversion);
public record ConvertResponse(bool conversionOk, string convertedCode, string errorMessage);

public static class WebConverter
{
    public static async Task<ConvertResponse> ConvertAsync(ConvertRequest todo, CancellationToken ct = default)
    {
        var languages = todo.requestedConversion.Split('2');

        string fromLanguage = LanguageNames.CSharp;
        string toLanguage = LanguageNames.VisualBasic;

        if (languages.Length == 2) {
            fromLanguage = ParseLanguage(languages[0]);
            toLanguage = ParseLanguage(languages[1]);
        }

        var codeWithOptions = new CodeWithOptions(todo.code)
            .WithTypeReferences(DefaultReferences.NetStandard2)
            .SetFromLanguage(fromLanguage)
            .SetToLanguage(toLanguage);

        var result = await CodeConverter.ConvertAsync(codeWithOptions, ct);

        return new ConvertResponse(result.Success, result.ConvertedCode, result.GetExceptionsAsString());
    }

    private static string ParseLanguage(string language)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (language.StartsWith("cs", StringComparison.OrdinalIgnoreCase)) {
            return LanguageNames.CSharp;
        }

        if (language.StartsWith("vb", StringComparison.OrdinalIgnoreCase)) {
            return LanguageNames.VisualBasic;
        }

        throw new ArgumentException($"{language} not supported!");
    }
}