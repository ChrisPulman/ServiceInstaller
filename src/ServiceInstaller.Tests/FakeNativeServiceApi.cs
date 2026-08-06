// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;
using ServiceInstaller.Internal;

namespace ServiceInstaller.Tests;

/// <summary>Records service-control operations without calling the Windows API.</summary>
internal sealed class FakeNativeServiceApi : IServiceNativeApi
{
    /// <summary>Gets the manager handles created by the fake.</summary>
    internal List<FakeServiceHandle> ManagerHandles { get; } = [];

    /// <summary>Gets the service handles created by the fake.</summary>
    internal List<FakeServiceHandle> ServiceHandles { get; } = [];

    /// <summary>Gets queued statuses returned by <see cref="QueryStatus"/>.</summary>
    internal Queue<ServiceStatus> Statuses { get; } = [];

    /// <summary>Gets or sets a value indicating whether opening a service succeeds.</summary>
    internal bool ServiceExists { get; set; } = true;

    /// <summary>Gets or sets the returned configuration.</summary>
    internal ServiceConfiguration Configuration { get; set; } = new(
        "service",
        "Service",
        "service.exe",
        ServiceStartMode.Automatic,
        "LocalSystem",
        null);

    /// <summary>Gets the most recent manager machine name.</summary>
    internal string? MachineName { get; private set; }

    /// <summary>Gets the most recent manager access rights.</summary>
    internal ScmAccessRights ManagerAccess { get; private set; }

    /// <summary>Gets the most recent service access rights.</summary>
    internal ServiceAccessRights ServiceAccess { get; private set; }

    /// <summary>Gets the created service definition.</summary>
    internal ServiceDefinition? CreatedDefinition { get; private set; }

    /// <summary>Gets the created image path.</summary>
    internal string? CreatedImagePath { get; private set; }

    /// <summary>Gets the created dependency multi-string.</summary>
    internal string? CreatedDependencies { get; private set; }

    /// <summary>Gets the configured update.</summary>
    internal ServiceUpdate? ChangedUpdate { get; private set; }

    /// <summary>Gets the configured image path.</summary>
    internal string? ChangedImagePath { get; private set; }

    /// <summary>Gets the configured dependency multi-string.</summary>
    internal string? ChangedDependencies { get; private set; }

    /// <summary>Gets the descriptions sent to the fake.</summary>
    internal List<string?> Descriptions { get; } = [];

    /// <summary>Gets the start arguments sent to the fake.</summary>
    internal List<IReadOnlyList<string>> StartArguments { get; } = [];

    /// <summary>Gets the controls sent to the fake.</summary>
    internal List<ServiceControl> Controls { get; } = [];

    /// <summary>Gets the number of delete calls.</summary>
    internal int DeleteCount { get; private set; }

    /// <summary>Opens a fake Service Control Manager handle.</summary>
    /// <param name="machineName">The optional remote machine name.</param>
    /// <param name="access">The requested manager access rights.</param>
    /// <returns>A new fake manager handle.</returns>
    public IServiceHandle OpenManager(string? machineName, ScmAccessRights access)
    {
        MachineName = machineName;
        ManagerAccess = access;
        var handle = new FakeServiceHandle();
        ManagerHandles.Add(handle);
        return handle;
    }

    /// <summary>Opens a fake service handle when the service exists.</summary>
    /// <param name="manager">The manager handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="access">The requested service access rights.</param>
    /// <returns>A new fake service handle, or null.</returns>
    public IServiceHandle? OpenService(IServiceHandle manager, string serviceName, ServiceAccessRights access)
    {
        ServiceAccess = access;
        return ServiceExists ? CreateServiceHandle() : null;
    }

    /// <summary>Creates and records a fake service.</summary>
    /// <param name="manager">The manager handle.</param>
    /// <param name="definition">The service definition.</param>
    /// <param name="imagePath">The executable command line.</param>
    /// <param name="dependencies">The dependency multi-string.</param>
    /// <returns>A new fake service handle.</returns>
    public IServiceHandle CreateService(
        IServiceHandle manager,
        ServiceDefinition definition,
        string imagePath,
        string? dependencies)
    {
        CreatedDefinition = definition;
        CreatedImagePath = imagePath;
        CreatedDependencies = dependencies;
        return CreateServiceHandle();
    }

    /// <summary>Returns the next configured service status.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The queued or stopped status.</returns>
    public ServiceStatus QueryStatus(IServiceHandle service, string serviceName) =>
        Statuses.Count == 0 ? new(ServiceState.Stopped) : Statuses.Dequeue();

    /// <summary>Returns the configured fake service configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The configured fake service configuration.</returns>
    public ServiceConfiguration QueryConfiguration(IServiceHandle service, string serviceName) => Configuration;

    /// <summary>Records a configuration change.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="update">The requested update.</param>
    /// <param name="imagePath">The optional executable command line.</param>
    /// <param name="dependencies">The optional dependency multi-string.</param>
    public void ChangeConfiguration(
        IServiceHandle service,
        string serviceName,
        ServiceUpdate update,
        string? imagePath,
        string? dependencies)
    {
        ChangedUpdate = update;
        ChangedImagePath = imagePath;
        ChangedDependencies = dependencies;
    }

    /// <summary>Records a service description update.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="description">The optional description.</param>
    public void SetDescription(IServiceHandle service, string serviceName, string? description) =>
        Descriptions.Add(description);

    /// <summary>Records a service start request.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="arguments">The start arguments.</param>
    public void Start(IServiceHandle service, string serviceName, IReadOnlyList<string> arguments) =>
        StartArguments.Add(arguments);

    /// <summary>Records a service control request.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="control">The requested service control.</param>
    public void Control(IServiceHandle service, string serviceName, ServiceControl control) => Controls.Add(control);

    /// <summary>Records a service deletion request.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The service name.</param>
    public void Delete(IServiceHandle service, string serviceName) => DeleteCount++;

    /// <summary>Creates and records a service handle.</summary>
    /// <returns>The owned service handle.</returns>
    private FakeServiceHandle CreateServiceHandle()
    {
        var handle = new FakeServiceHandle();
        ServiceHandles.Add(handle);
        return handle;
    }
}
