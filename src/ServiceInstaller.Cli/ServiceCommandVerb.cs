// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Defines parsed terminal commands.</summary>
internal enum ServiceCommandVerb
{
    /// <summary>An unknown command.</summary>
    Unknown,

    /// <summary>Create a service.</summary>
    Create,

    /// <summary>Create and start a service.</summary>
    Install,

    /// <summary>Configure a service.</summary>
    Configure,

    /// <summary>Query a service.</summary>
    Query,

    /// <summary>Query a service through its status alias.</summary>
    Status,

    /// <summary>Check whether a service exists.</summary>
    Exists,

    /// <summary>Start a service.</summary>
    Start,

    /// <summary>Stop a service.</summary>
    Stop,

    /// <summary>Pause a service.</summary>
    Pause,

    /// <summary>Continue a service.</summary>
    Continue,

    /// <summary>Continue a service through its resume alias.</summary>
    Resume,

    /// <summary>Delete a service.</summary>
    Delete,

    /// <summary>Delete a service through its uninstall alias.</summary>
    Uninstall,
}
