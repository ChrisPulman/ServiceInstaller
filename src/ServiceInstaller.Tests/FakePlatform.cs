// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Internal;

namespace ServiceInstaller.Tests;

/// <summary>Provides a controllable platform capability result.</summary>
internal sealed class FakePlatform : IPlatform
{
    /// <summary>Initializes a new instance of the <see cref="FakePlatform"/> class.</summary>
    /// <param name="isWindows">Whether Windows service management is available.</param>
    internal FakePlatform(bool isWindows = true) => IsWindows = isWindows;

    /// <summary>Gets a value indicating whether the platform is Windows.</summary>
    public bool IsWindows { get; }
}
