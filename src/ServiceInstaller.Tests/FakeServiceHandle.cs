// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Internal;

namespace ServiceInstaller.Tests;

/// <summary>Provides a disposable fake native service handle.</summary>
internal sealed class FakeServiceHandle : IServiceHandle
{
    /// <summary>Gets a value indicating whether the handle was disposed.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Releases the fake handle.</summary>
    public void Dispose() => IsDisposed = true;
}
