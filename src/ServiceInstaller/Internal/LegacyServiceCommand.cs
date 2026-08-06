// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Defines parsed legacy service commands.</summary>
internal enum LegacyServiceCommand
{
    /// <summary>An unknown command.</summary>
    Unknown,

    /// <summary>Install and start a service.</summary>
    Install,

    /// <summary>Delete a service.</summary>
    Uninstall,

    /// <summary>Start a service.</summary>
    Start,

    /// <summary>Stop a service.</summary>
    Stop,

    /// <summary>Pause a service.</summary>
    Pause,

    /// <summary>Continue a service.</summary>
    Continue,

    /// <summary>Continue through the resume alias.</summary>
    Resume,

    /// <summary>Query service status.</summary>
    Status,

    /// <summary>Check whether a service exists.</summary>
    IsInstalled,
}
