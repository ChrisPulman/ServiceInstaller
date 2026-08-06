// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>Identifies the control messages that a running service accepts.</summary>
[Flags]
public enum ServiceAcceptedControls
{
    /// <summary>The service does not accept control messages.</summary>
    None = 0,

    /// <summary>The service can be stopped.</summary>
    Stop = 0x00000001,

    /// <summary>The service can be paused and continued.</summary>
    PauseContinue = 0x00000002,

    /// <summary>The service receives system shutdown notifications.</summary>
    Shutdown = 0x00000004,

    /// <summary>The service receives parameter-change notifications.</summary>
    ParameterChange = 0x00000008,

    /// <summary>The service receives network binding notifications.</summary>
    NetworkBindingChange = 0x00000010,

    /// <summary>The service receives hardware profile change notifications.</summary>
    HardwareProfileChange = 0x00000020,

    /// <summary>The service receives power event notifications.</summary>
    PowerEvent = 0x00000040,

    /// <summary>The service receives session change notifications.</summary>
    SessionChange = 0x00000080,

    /// <summary>The service can receive pre-shutdown notifications.</summary>
    PreShutdown = 0x00000100,

    /// <summary>The service receives time-change notifications.</summary>
    TimeChange = 0x00000200,

    /// <summary>The service receives trigger-event notifications.</summary>
    TriggerEvent = 0x00000400,
}
