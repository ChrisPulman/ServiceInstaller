// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Reports platform capabilities.</summary>
internal interface IPlatform
{
    /// <summary>Gets a value indicating whether the current platform is Windows.</summary>
    bool IsWindows { get; }
}
