// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller.Tests;

/// <summary>Records service-manager calls for adapter and CLI tests.</summary>
internal sealed class RecordingServiceManager : IServiceManager
{
    /// <summary>Gets or sets a value indicating whether deletion succeeds.</summary>
    internal bool DeleteResult { get; set; } = true;

    /// <summary>Gets the last created definition.</summary>
    internal ServiceDefinition? Definition { get; private set; }

    /// <summary>Gets or sets a value indicating whether the service exists.</summary>
    internal bool ExistsResult { get; set; }

    /// <summary>Gets or sets an exception to throw.</summary>
    internal Exception? Failure { get; set; }

    /// <summary>Gets recorded operation names.</summary>
    internal List<string> Operations { get; } = [];

    /// <summary>Gets the last runtime start arguments.</summary>
    internal string[] StartArguments { get; private set; } = [];

    /// <summary>Gets the last operation timeout.</summary>
    internal TimeSpan Timeout { get; private set; }

    /// <summary>Gets the last configuration update.</summary>
    internal ServiceUpdate? Update { get; private set; }

    /// <summary>Gets or sets the query result.</summary>
    internal ServiceSnapshot Snapshot { get; set; } = new(
        new ServiceConfiguration("sample", "Sample", "\"service.exe\"", ServiceStartMode.Automatic, "LocalSystem", null),
        new ServiceStatus(ServiceState.Running));

    /// <summary>Gets or sets the transition result.</summary>
    internal ServiceStatus Status { get; set; } = new(ServiceState.Running);

    /// <inheritdoc/>
    public void Configure(string serviceName, ServiceUpdate update)
    {
        ThrowWhenConfigured();
        Operations.Add($"configure:{serviceName}");
        Update = update;
    }

    /// <inheritdoc/>
    public void Create(ServiceDefinition definition)
    {
        ThrowWhenConfigured();
        Operations.Add($"create:{definition.ServiceName}");
        Definition = definition;
    }

    /// <inheritdoc/>
    public bool Delete(string serviceName, TimeSpan timeout)
    {
        ThrowWhenConfigured();
        Operations.Add($"delete:{serviceName}");
        Timeout = timeout;
        return DeleteResult;
    }

    /// <inheritdoc/>
    public bool Exists(string serviceName)
    {
        ThrowWhenConfigured();
        Operations.Add($"exists:{serviceName}");
        return ExistsResult;
    }

    /// <inheritdoc/>
    public ServiceStatus Continue(string serviceName, TimeSpan timeout) =>
        Transition("continue", serviceName, timeout);

    /// <inheritdoc/>
    public ServiceStatus Pause(string serviceName, TimeSpan timeout) =>
        Transition("pause", serviceName, timeout);

    /// <inheritdoc/>
    public ServiceSnapshot Query(string serviceName)
    {
        ThrowWhenConfigured();
        Operations.Add($"query:{serviceName}");
        return Snapshot;
    }

    /// <inheritdoc/>
    public ServiceStatus Start(string serviceName, TimeSpan timeout, params string[] arguments)
    {
        StartArguments = arguments;
        return Transition("start", serviceName, timeout);
    }

    /// <inheritdoc/>
    public ServiceStatus Stop(string serviceName, TimeSpan timeout) =>
        Transition("stop", serviceName, timeout);

    /// <summary>Records a state transition.</summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="timeout">The transition timeout.</param>
    /// <returns>The configured status.</returns>
    private ServiceStatus Transition(string operation, string serviceName, TimeSpan timeout)
    {
        ThrowWhenConfigured();
        Operations.Add($"{operation}:{serviceName}");
        Timeout = timeout;
        return Status;
    }

    /// <summary>Throws the configured failure.</summary>
    private void ThrowWhenConfigured()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
