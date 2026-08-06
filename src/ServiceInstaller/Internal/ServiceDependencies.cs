// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Builds the double-null-terminated dependency format expected by Windows.</summary>
internal static class ServiceDependencies
{
    /// <summary>Converts service names into a native multi-string.</summary>
    /// <param name="dependencies">The service dependency names, or null for no change.</param>
    /// <returns>A double-null-terminated multi-string, or null.</returns>
    internal static string? ToMultiString(IReadOnlyList<string>? dependencies)
    {
        if (dependencies is null)
        {
            return null;
        }

        return dependencies.Count == 0 ? "\0" : $"{string.Join("\0", dependencies)}\0\0";
    }
}
