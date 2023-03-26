// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

internal enum ServiceBootFlag
{
    Start = 0x00000000,
    SystemStart = 0x00000001,
    AutoStart = 0x00000002,
    DemandStart = 0x00000003,
    Disabled = 0x00000004
}
