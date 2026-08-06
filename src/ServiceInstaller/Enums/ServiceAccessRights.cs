// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>Defines service access rights.</summary>
[Flags]
internal enum ServiceAccessRights
{
    /// <summary>Grants no access.</summary>
    None = 0,

    /// <summary>Grants configuration-query access.</summary>
    QueryConfig = 0x0001,

    /// <summary>Grants configuration-change access.</summary>
    ChangeConfig = 0x0002,

    /// <summary>Grants status-query access.</summary>
    QueryStatus = 0x0004,

    /// <summary>Grants start access.</summary>
    Start = 0x0010,

    /// <summary>Grants stop access.</summary>
    Stop = 0x0020,

    /// <summary>Grants pause and continue access.</summary>
    PauseContinue = 0x0040,

    /// <summary>Grants deletion access.</summary>
    Delete = 0x00010000,
}
