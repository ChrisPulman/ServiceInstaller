// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller;

/// <summary>Contains optional changes to an installed service.</summary>
public sealed class ServiceUpdate
{
    /// <summary>Gets or sets a replacement display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets a replacement executable path.</summary>
    public string? ExecutablePath { get; set; }

    /// <summary>Gets or sets replacement executable arguments.</summary>
    public IReadOnlyList<string>? Arguments { get; set; }

    /// <summary>Gets or sets a replacement start mode.</summary>
    public ServiceStartMode? StartMode { get; set; }

    /// <summary>Gets or sets a replacement description. An empty value clears it.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the description should be changed.</summary>
    public bool ChangeDescription { get; set; }

    /// <summary>Gets or sets a replacement service account name.</summary>
    public string? AccountName { get; set; }

    /// <summary>Gets or sets the replacement account password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets whether the service account should be changed.</summary>
    public bool ChangeAccount { get; set; }

    /// <summary>Gets or sets replacement dependencies.</summary>
    public IReadOnlyList<string>? Dependencies { get; set; }
}
