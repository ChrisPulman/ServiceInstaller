// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

/// <summary>Defines Service Control Manager access rights.</summary>
[Flags]
internal enum ScmAccessRights
{
    /// <summary>Grants no access.</summary>
    None = 0,

    /// <summary>Grants connection access.</summary>
    Connect = 0x0001,

    /// <summary>Grants service-creation access.</summary>
    CreateService = 0x0002,
}
