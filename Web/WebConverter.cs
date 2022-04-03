﻿using System;
using System.Threading.Tasks;
using ICSharpCode.CodeConverter.Shared;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.CodeConverter.Web;

public record ConvertRequest(string code, string requestedConversion);
public record ConvertResponse(bool conversionOk, string convertedCode, string errorMessage);

public static class WebConverter
{
    public static async Task<ConvertResponse> ConvertAsync(ConvertRequest todo)
    {
        var languages = todo.requestedConversion.Split('2');

        string fromLanguage = LanguageNames.CSharp;
        string toLanguage = LanguageNames.VisualBasic;
        int fromVersion = 6;
        int toVersion = 14;

        if (languages.Length == 2) {
            fromLanguage = ParseLanguage(languages[0]);
            fromVersion = GetDefaultVersionForLanguage(languages[0]);
            toLanguage = ParseLanguage(languages[1]);
            toVersion = GetDefaultVersionForLanguage(languages[1]);
        }

        var codeWithOptions = new CodeWithOptions(todo.code)
            .WithTypeReferences(DefaultReferences.NetStandard2)
            .SetFromLanguage(fromLanguage, fromVersion)
            .SetToLanguage(toLanguage, toVersion);

        var result = await CodeConverter.ConvertAsync(codeWithOptions);

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

    private static int GetDefaultVersionForLanguage(string language)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (language.StartsWith("cs", StringComparison.OrdinalIgnoreCase)) {
            return 6;
        }

        if (language.StartsWith("vb", StringComparison.OrdinalIgnoreCase)) {
            return 14;
        }

        throw new ArgumentException($"{language} not supported!");
    }
}