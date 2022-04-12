// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace ICSharpCode.CodeConverter.Util.FromRoslyn;

internal static class INamespaceOrTypeSymbolExtensions
{
    private static readonly SymbolDisplayFormat ShortNameFormat = new(
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

    public static string GetShortName(this INamespaceOrTypeSymbol symbol)
    {
        return symbol.ToDisplayString(ShortNameFormat);
    }
}