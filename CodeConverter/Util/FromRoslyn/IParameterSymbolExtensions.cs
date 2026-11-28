// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ICSharpCode.CodeConverter.Util.FromRoslyn;

internal static class IParameterSymbolExtensions
{
    public static bool IsRefOrOut(this IParameterSymbol symbol)
    {
        switch (symbol.RefKind) {
            case RefKind.Ref:
            case RefKind.Out:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// ExplicitDefaultValue throws where the default isn't a valid constant of the parameter's type, e.g. one that overflows it (BC30439)
    /// </summary>
    /// <returns>Null where there's no default, it's Nothing, or it's invalid</returns>
    public static object GetValidExplicitDefaultValueOrNull(this IParameterSymbol symbol)
    {
        if (!symbol.HasExplicitDefaultValue) return null;
        try {
            return symbol.ExplicitDefaultValue;
        } catch (InvalidOperationException) {
            return null;
        }
    }
}