// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>
/// Service State.
/// </summary>
public enum ServiceState
{
    /// <summary>
    /// The state cannot be (has not been) retrieved.
    /// </summary>
    Unknown = -1,
    /// <summary>
    /// The service is not known on the host server.
    /// </summary>
    NotFound = 0,
    /// <summary>
    /// The service is stopped.
    /// </summary>
    Stopped = 1,
    /// <summary>
    /// The  service is start pending.
    /// </summary>
    StartPending = 2,
    /// <summary>
    /// The  service is stop pending.
    /// </summary>
    StopPending = 3,
    /// <summary>
    /// The  service is running.
    /// </summary>
    Running = 4,
    /// <summary>
    /// The  service is continue pending.
    /// </summary>
    ContinuePending = 5,
    /// <summary>
    /// The  service is pause pending.
    /// </summary>
    PausePending = 6,
    /// <summary>
    /// The  service is paused.
    /// </summary>
    Paused = 7,
}
