// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller;

/// <summary>Defines a Windows service to create.</summary>
public sealed class ServiceDefinition
{
    /// <summary>Initializes a new instance of the <see cref="ServiceDefinition"/> class.</summary>
    /// <param name="serviceName">The stable service name used by the Service Control Manager.</param>
    /// <param name="displayName">The human-readable service display name.</param>
    /// <param name="executablePath">The executable that hosts the service.</param>
    public ServiceDefinition(string serviceName, string displayName, string executablePath)
    {
        ServiceName = serviceName;
        DisplayName = displayName;
        ExecutablePath = executablePath;
    }

    /// <summary>Gets the stable service name.</summary>
    public string ServiceName { get; }

    /// <summary>Gets the display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the service executable path.</summary>
    public string ExecutablePath { get; }

    /// <summary>Gets or sets the command-line arguments passed to the executable.</summary>
    public IReadOnlyList<string> Arguments { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the service start mode.</summary>
    public ServiceStartMode StartMode { get; set; } = ServiceStartMode.Automatic;

    /// <summary>Gets or sets the optional service description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional service account name. A null value uses LocalSystem.</summary>
    public string? AccountName { get; set; }

    /// <summary>Gets or sets the optional password for <see cref="AccountName"/>.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the service names that must start before this service.</summary>
    public IReadOnlyList<string> Dependencies { get; set; } = Array.Empty<string>();
}
