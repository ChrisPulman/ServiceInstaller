// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller;

/// <summary>Describes the current runtime status of a service.</summary>
public sealed class ServiceStatus
{
    /// <summary>Initializes a new instance of the <see cref="ServiceStatus"/> class.</summary>
    /// <param name="state">The current service state.</param>
    public ServiceStatus(ServiceState state)
        : this(state, ServiceAcceptedControls.None, 0, 0, 0)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ServiceStatus"/> class.</summary>
    /// <param name="state">The current service state.</param>
    /// <param name="acceptedControls">The controls accepted by the service.</param>
    /// <param name="processId">The service process identifier, or zero when no process is active.</param>
    /// <param name="checkpoint">The progress checkpoint for a pending operation.</param>
    /// <param name="waitHintMilliseconds">The estimated pending-operation duration.</param>
    public ServiceStatus(
        ServiceState state,
        ServiceAcceptedControls acceptedControls,
        uint processId,
        uint checkpoint,
        uint waitHintMilliseconds)
    {
        State = state;
        AcceptedControls = acceptedControls;
        ProcessId = processId;
        Checkpoint = checkpoint;
        WaitHintMilliseconds = waitHintMilliseconds;
    }

    /// <summary>Gets the current state.</summary>
    public ServiceState State { get; }

    /// <summary>Gets the controls accepted by the service.</summary>
    public ServiceAcceptedControls AcceptedControls { get; }

    /// <summary>Gets the process identifier.</summary>
    public uint ProcessId { get; }

    /// <summary>Gets the progress checkpoint.</summary>
    public uint Checkpoint { get; }

    /// <summary>Gets the wait hint in milliseconds.</summary>
    public uint WaitHintMilliseconds { get; }
}
