// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////#define TRACKDEPTH

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn
{

    internal static class MethodKindExtensions
    {
        public static bool IsPropertyAccessor(this MethodKind kind)
        {
            return kind == MethodKind.PropertyGet || kind == MethodKind.PropertySet;
        }
    }
}
