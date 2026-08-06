// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller;

/// <summary>Describes an installed service configuration.</summary>
public sealed class ServiceConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="ServiceConfiguration"/> class.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="imagePath">The configured executable command line.</param>
    /// <param name="startMode">The configured start mode.</param>
    /// <param name="accountName">The configured service account.</param>
    /// <param name="description">The optional service description.</param>
    public ServiceConfiguration(
        string serviceName,
        string displayName,
        string imagePath,
        ServiceStartMode startMode,
        string accountName,
        string? description)
    {
        ServiceName = serviceName;
        DisplayName = displayName;
        ImagePath = imagePath;
        StartMode = startMode;
        AccountName = accountName;
        Description = description;
    }

    /// <summary>Gets the stable service name.</summary>
    public string ServiceName { get; }

    /// <summary>Gets the display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the configured executable command line.</summary>
    public string ImagePath { get; }

    /// <summary>Gets the configured start mode.</summary>
    public ServiceStartMode StartMode { get; }

    /// <summary>Gets the configured service account.</summary>
    public string AccountName { get; }

    /// <summary>Gets the optional service description.</summary>
    public string? Description { get; }
}
