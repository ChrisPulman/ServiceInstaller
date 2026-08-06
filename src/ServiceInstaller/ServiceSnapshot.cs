// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller;

/// <summary>Combines an installed service's configuration and runtime status.</summary>
public sealed class ServiceSnapshot
{
    /// <summary>Initializes a new instance of the <see cref="ServiceSnapshot"/> class.</summary>
    /// <param name="configuration">The installed configuration.</param>
    /// <param name="status">The runtime status.</param>
    public ServiceSnapshot(ServiceConfiguration configuration, ServiceStatus status)
    {
        Configuration = configuration;
        Status = status;
    }

    /// <summary>Gets the installed configuration.</summary>
    public ServiceConfiguration Configuration { get; }

    /// <summary>Gets the runtime status.</summary>
    public ServiceStatus Status { get; }
}
