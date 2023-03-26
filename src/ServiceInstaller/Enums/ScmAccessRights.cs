// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

[Flags]
internal enum ScmAccessRights
{
    None = 0,
    Connect = 0x0001,
    CreateService = 0x0002,
    EnumerateService = 0x0004,
    Lock = 0x0008,
    QueryLockStatus = 0x0010,
    ModifyBootConfig = 0x0020,
    StandardRightsRequired = 0xF0000,
    AllAccess = StandardRightsRequired | Connect | CreateService |
                 EnumerateService | Lock | QueryLockStatus | ModifyBootConfig
}
