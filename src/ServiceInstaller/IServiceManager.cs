// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller;

/// <summary>Creates, configures, queries, and controls Windows services.</summary>
public interface IServiceManager
{
    /// <summary>Determines whether a service is installed.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <returns><see langword="true"/> when the service exists.</returns>
    bool Exists(string serviceName);

    /// <summary>Creates a service without starting it.</summary>
    /// <param name="definition">The service definition.</param>
    void Create(ServiceDefinition definition);

    /// <summary>Changes an installed service configuration.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="update">The requested changes.</param>
    void Configure(string serviceName, ServiceUpdate update);

    /// <summary>Queries an installed service.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <returns>The configuration and runtime status.</returns>
    ServiceSnapshot Query(string serviceName);

    /// <summary>Starts an installed service and waits for it to run.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="timeout">The maximum transition duration.</param>
    /// <param name="arguments">Optional one-time start arguments.</param>
    /// <returns>The final service status.</returns>
    ServiceStatus Start(string serviceName, TimeSpan timeout, params string[] arguments);

    /// <summary>Stops an installed service and waits for it to stop.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="timeout">The maximum transition duration.</param>
    /// <returns>The final service status.</returns>
    ServiceStatus Stop(string serviceName, TimeSpan timeout);

    /// <summary>Pauses an installed service and waits for it to pause.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="timeout">The maximum transition duration.</param>
    /// <returns>The final service status.</returns>
    ServiceStatus Pause(string serviceName, TimeSpan timeout);

    /// <summary>Continues a paused service and waits for it to run.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="timeout">The maximum transition duration.</param>
    /// <returns>The final service status.</returns>
    ServiceStatus Continue(string serviceName, TimeSpan timeout);

    /// <summary>Stops and deletes an installed service.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="timeout">The maximum stop duration.</param>
    /// <returns><see langword="true"/> when a service was marked for deletion.</returns>
    bool Delete(string serviceName, TimeSpan timeout);
}
