// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Reports the runtime operating system.</summary>
internal sealed class SystemPlatform : IPlatform
{
    /// <inheritdoc/>
    public bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
}
