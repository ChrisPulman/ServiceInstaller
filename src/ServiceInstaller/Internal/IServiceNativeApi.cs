// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller.Internal;

/// <summary>Abstracts the native Windows Service Control Manager API.</summary>
internal interface IServiceNativeApi
{
    /// <summary>Opens the Service Control Manager.</summary>
    /// <param name="machineName">The optional remote computer name.</param>
    /// <param name="access">The required manager access.</param>
    /// <returns>An owned manager handle.</returns>
    IServiceHandle OpenManager(string? machineName, ScmAccessRights access);

    /// <summary>Opens an installed service, or returns null when it does not exist.</summary>
    /// <param name="manager">The manager handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="access">The required service access.</param>
    /// <returns>An owned service handle, or null.</returns>
    IServiceHandle? OpenService(IServiceHandle manager, string serviceName, ServiceAccessRights access);

    /// <summary>Creates a service and returns its owned handle.</summary>
    /// <param name="manager">The manager handle.</param>
    /// <param name="definition">The service definition.</param>
    /// <param name="imagePath">The escaped image path.</param>
    /// <param name="dependencies">The native dependency multi-string.</param>
    /// <returns>An owned service handle.</returns>
    IServiceHandle CreateService(
        IServiceHandle manager,
        ServiceDefinition definition,
        string imagePath,
        string? dependencies);

    /// <summary>Queries the current runtime status.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <returns>The runtime status.</returns>
    ServiceStatus QueryStatus(IServiceHandle service, string serviceName);

    /// <summary>Queries the installed configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <returns>The installed configuration.</returns>
    ServiceConfiguration QueryConfiguration(IServiceHandle service, string serviceName);

    /// <summary>Changes the installed configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="update">The requested changes.</param>
    /// <param name="imagePath">The optional escaped image path.</param>
    /// <param name="dependencies">The optional dependency multi-string.</param>
    void ChangeConfiguration(
        IServiceHandle service,
        string serviceName,
        ServiceUpdate update,
        string? imagePath,
        string? dependencies);

    /// <summary>Sets or clears the service description.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="description">The optional description.</param>
    void SetDescription(IServiceHandle service, string serviceName, string? description);

    /// <summary>Starts the service.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="arguments">The one-time start arguments.</param>
    void Start(IServiceHandle service, string serviceName, IReadOnlyList<string> arguments);

    /// <summary>Sends a control message to the service.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="control">The control message.</param>
    void Control(IServiceHandle service, string serviceName, ServiceControl control);

    /// <summary>Marks the service for deletion.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    void Delete(IServiceHandle service, string serviceName);
}
