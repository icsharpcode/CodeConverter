// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace ICSharpCode.CodeConverter.Util.FromRoslyn;

internal static class Hash
{
    /// <summary>
    /// This is how VB Anonymous Types combine hash values for fields.
    /// </summary>
    internal static int Combine(int newKey, int currentKey)
    {
        return unchecked((currentKey * (int)0xA5555529) + newKey);
    }

    internal static int Combine(bool newKeyPart, int currentKey)
    {
        return Combine(currentKey, newKeyPart ? 1 : 0);
    }

    /// <summary>
    /// This is how VB Anonymous Types combine hash values for fields.
    /// PERF: Do not use with enum types because that involves multiple
    /// unnecessary boxing operations.  Unfortunately, we can't constrain
    /// T to "non-enum", so we'll use a more restrictive constraint.
    /// </summary>
    internal static int Combine<T>(T newKeyPart, int currentKey) where T : class?
    {
        int hash = unchecked(currentKey * (int)0xA5555529);

        if (newKeyPart != null)
        {
            return unchecked(hash + newKeyPart.GetHashCode());
        }

        return hash;
    }

    internal static int CombineValues<T>(ImmutableArray<T> values, int maxItemsToHash = int.MaxValue)
    {
        if (values.IsDefaultOrEmpty)
        {
            return 0;
        }

        var hashCode = 0;
        var count = 0;
        foreach (var value in values)
        {
            if (count++ >= maxItemsToHash)
            {
                break;
            }

            // Should end up with a constrained virtual call to object.GetHashCode (i.e. avoid boxing where possible).
            if (value != null)
            {
                hashCode = Hash.Combine(value.GetHashCode(), hashCode);
            }
        }

        return hashCode;
    }
}