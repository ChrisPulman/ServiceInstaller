// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Provides monotonic elapsed time and bounded delays.</summary>
internal interface IWaitStrategy
{
    /// <summary>Gets elapsed milliseconds from a monotonic clock.</summary>
    long ElapsedMilliseconds { get; }

    /// <summary>Delays the caller by the requested milliseconds.</summary>
    /// <param name="milliseconds">The delay in milliseconds.</param>
    void Delay(int milliseconds);
}
