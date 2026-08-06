// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;
using ServiceInstaller.Internal;

namespace ServiceInstaller;

/// <summary>Owns a native service or Service Control Manager handle.</summary>
[ExcludeFromCodeCoverage]
internal sealed class NativeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid, IServiceHandle
{
    /// <summary>Initializes a new instance of the <see cref="NativeServiceHandle"/> class.</summary>
    public NativeServiceHandle()
        : base(true)
    {
    }

    /// <summary>Closes the native handle.</summary>
    /// <returns>True when the handle was closed.</returns>
    protected override bool ReleaseHandle() => NativeMethods.CloseServiceHandle(handle);
}
