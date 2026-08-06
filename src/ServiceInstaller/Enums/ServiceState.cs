// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>Defines Windows service runtime states.</summary>
public enum ServiceState
{
    /// <summary>The state has not been retrieved.</summary>
    Unknown = -1,

    /// <summary>The service is not installed.</summary>
    NotFound = 0,

    /// <summary>The service is stopped.</summary>
    Stopped = 1,

    /// <summary>The service is starting.</summary>
    StartPending = 2,

    /// <summary>The service is stopping.</summary>
    StopPending = 3,

    /// <summary>The service is running.</summary>
    Running = 4,

    /// <summary>The service is continuing.</summary>
    ContinuePending = 5,

    /// <summary>The service is pausing.</summary>
    PausePending = 6,

    /// <summary>The service is paused.</summary>
    Paused = 7,
}
