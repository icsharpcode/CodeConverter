// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{

    internal static partial class IParameterSymbolExtensions
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
}
