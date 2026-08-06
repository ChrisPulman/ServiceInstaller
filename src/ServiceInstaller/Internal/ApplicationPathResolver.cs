// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ServiceInstaller.Internal;

/// <summary>Resolves the current native process image path.</summary>
internal sealed class ApplicationPathResolver : IApplicationPathResolver
{
    /// <summary>The current-process path provider.</summary>
    private readonly Func<string?> _processPathProvider;

    /// <summary>Initializes a new instance of the <see cref="ApplicationPathResolver"/> class.</summary>
    internal ApplicationPathResolver()
        : this(GetProcessPath)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ApplicationPathResolver"/> class.</summary>
    /// <param name="processPathProvider">The current-process path provider.</param>
    internal ApplicationPathResolver(Func<string?> processPathProvider) =>
        _processPathProvider = Guard.NotNull(processPathProvider, nameof(processPathProvider));

    /// <inheritdoc/>
    public string Resolve()
    {
        var path = _processPathProvider();
        return string.IsNullOrWhiteSpace(path) ? Environment.GetCommandLineArgs()[0] : path!;
    }

    /// <summary>Reads the native process image path.</summary>
    /// <returns>The process image path, when available.</returns>
    [ExcludeFromCodeCoverage]
    private static string? GetProcessPath()
    {
        using var process = Process.GetCurrentProcess();
        return process.MainModule?.FileName;
    }
}
