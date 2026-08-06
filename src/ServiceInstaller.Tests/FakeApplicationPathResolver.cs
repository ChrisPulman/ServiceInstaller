// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Internal;

namespace ServiceInstaller.Tests;

/// <summary>Resolves a deterministic application path for tests.</summary>
/// <param name="path">The path to return.</param>
internal sealed class FakeApplicationPathResolver(string path) : IApplicationPathResolver
{
    /// <inheritdoc/>
    public string Resolve() => path;
}
