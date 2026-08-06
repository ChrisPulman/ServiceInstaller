// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>Defines service control messages.</summary>
internal enum ServiceControl
{
    /// <summary>Requests that a service stop.</summary>
    Stop = 0x00000001,

    /// <summary>Requests that a service pause.</summary>
    Pause = 0x00000002,

    /// <summary>Requests that a service continue.</summary>
    Continue = 0x00000003,
}
