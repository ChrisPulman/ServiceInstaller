// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>Specifies when the Windows Service Control Manager starts a service.</summary>
public enum ServiceStartMode
{
    /// <summary>Represents an unspecified start mode.</summary>
    Unknown = 0,

    /// <summary>The service starts automatically during system startup.</summary>
    Automatic = 2,

    /// <summary>The service starts only when requested.</summary>
    Manual = 3,

    /// <summary>The service cannot be started until it is reconfigured.</summary>
    Disabled = 4,
}
