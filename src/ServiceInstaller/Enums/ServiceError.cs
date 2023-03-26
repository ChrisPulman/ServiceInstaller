// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ServiceInstaller.Enums;

internal enum ServiceError
{
    Ignore = 0x00000000,
    Normal = 0x00000001,
    Severe = 0x00000002,
    Critical = 0x00000003
}