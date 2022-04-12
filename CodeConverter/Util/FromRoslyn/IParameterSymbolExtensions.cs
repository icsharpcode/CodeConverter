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
}