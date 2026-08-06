// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Internal;

namespace ServiceInstaller.Tests;

/// <summary>Provides deterministic monotonic time for lifecycle tests.</summary>
internal sealed class FakeWaitStrategy : IWaitStrategy
{
    /// <summary>Gets the elapsed monotonic time in milliseconds.</summary>
    public long ElapsedMilliseconds { get; private set; }

    /// <summary>Gets the requested wait intervals.</summary>
    internal List<int> Delays { get; } = [];

    /// <summary>Advances time by the supplied duration.</summary>
    /// <param name="milliseconds">The requested delay in milliseconds.</param>
    public void Delay(int milliseconds)
    {
        Delays.Add(milliseconds);
        ElapsedMilliseconds += milliseconds;
    }
}
