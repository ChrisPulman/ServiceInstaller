// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;

namespace ServiceInstaller.Internal;

/// <summary>Builds a correctly escaped Windows process command line.</summary>
internal static class WindowsCommandLine
{
    /// <summary>The multiplier used when escaping backslashes before a quote.</summary>
    private const int QuoteBackslashMultiplier = 2;

    /// <summary>Builds a quoted executable path and its escaped arguments.</summary>
    /// <param name="executablePath">The service executable path.</param>
    /// <param name="arguments">The stored service arguments.</param>
    /// <returns>The escaped Windows command line.</returns>
    internal static string Build(string executablePath, IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder(Quote(executablePath, true));
        foreach (var argument in arguments)
        {
            _ = builder.Append(' ').Append(Quote(argument, false));
        }

        return builder.ToString();
    }

    /// <summary>Quotes one Windows command-line argument when required.</summary>
    /// <param name="value">The argument value.</param>
    /// <param name="alwaysQuote">Whether to quote even a simple value.</param>
    /// <returns>The escaped argument.</returns>
    private static string Quote(string value, bool alwaysQuote)
    {
        var needsQuotes = alwaysQuote || value.Length == 0 || ContainsWhitespaceOrQuote(value);
        if (!needsQuotes)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + QuoteBackslashMultiplier);
        _ = builder.Append('"');
        var backslashes = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                _ = builder
                    .Append('\\', (backslashes * QuoteBackslashMultiplier) + 1)
                    .Append(character);
                backslashes = 0;
                continue;
            }

            _ = builder.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        return builder
            .Append('\\', backslashes * QuoteBackslashMultiplier)
            .Append('"')
            .ToString();
    }

    /// <summary>Determines whether a value contains whitespace or a quote.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>True when quoting is required.</returns>
    private static bool ContainsWhitespaceOrQuote(string value)
    {
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character) || character == '"')
            {
                return true;
            }
        }

        return false;
    }
}
