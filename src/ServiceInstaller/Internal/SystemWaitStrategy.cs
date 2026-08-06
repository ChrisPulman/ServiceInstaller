// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace ServiceInstaller.Internal;

/// <summary>Uses a stopwatch and thread delay for production service transitions.</summary>
internal sealed class SystemWaitStrategy : IWaitStrategy
{
    /// <summary>Tracks monotonic elapsed time.</summary>
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <inheritdoc/>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <inheritdoc/>
    public void Delay(int milliseconds) => Thread.Sleep(milliseconds);
}
