// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Provides guard clauses that work on every supported target framework.</summary>
internal static class Guard
{
    /// <summary>Returns a reference or throws when it is null.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The reference to validate.</param>
    /// <param name="parameterName">The caller parameter name.</param>
    /// <returns>The validated reference.</returns>
    internal static T NotNull<T>(T? value, string parameterName)
        where T : class => value ?? throw new ArgumentNullException(parameterName);

    /// <summary>Returns text or throws when it is empty or whitespace.</summary>
    /// <param name="value">The text to validate.</param>
    /// <param name="parameterName">The caller parameter name.</param>
    /// <returns>The validated text.</returns>
    internal static string NotNullOrWhiteSpace(string? value, string parameterName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
#else
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The value cannot be empty or whitespace.", parameterName);
        }

        return value!;
#endif
    }
}
