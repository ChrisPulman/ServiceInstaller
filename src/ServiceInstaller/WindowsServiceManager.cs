// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;
using ServiceInstaller.Internal;

namespace ServiceInstaller;

/// <summary>Manages services through the Windows Service Control Manager.</summary>
public sealed class WindowsServiceManager : IServiceManager
{
    /// <summary>The Win32 missing-service error.</summary>
    private const int MissingServiceError = 1060;

    /// <summary>The minimum status polling interval.</summary>
    private const long MinimumPollMilliseconds = 100;

    /// <summary>The maximum status polling interval.</summary>
    private const long MaximumPollMilliseconds = 1000;

    /// <summary>The divisor applied to a native wait hint.</summary>
    private const long WaitHintDivisor = 10;

    /// <summary>The default service transition timeout.</summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The native API boundary.</summary>
    private readonly IServiceNativeApi _nativeApi;

    /// <summary>The monotonic wait strategy.</summary>
    private readonly IWaitStrategy _waitStrategy;

    /// <summary>The platform capability provider.</summary>
    private readonly IPlatform _platform;

    /// <summary>The optional remote computer name.</summary>
    private readonly string? _machineName;

    /// <summary>Initializes a new instance of the <see cref="WindowsServiceManager"/> class for the local computer.</summary>
    public WindowsServiceManager()
        : this(null)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WindowsServiceManager"/> class.</summary>
    /// <param name="machineName">An optional remote computer name.</param>
    public WindowsServiceManager(string? machineName)
        : this(new NativeServiceApi(), new SystemWaitStrategy(), new SystemPlatform(), machineName)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WindowsServiceManager"/> class.</summary>
    /// <param name="nativeApi">The native API boundary.</param>
    /// <param name="waitStrategy">The monotonic wait strategy.</param>
    /// <param name="platform">The platform capability provider.</param>
    /// <param name="machineName">The optional remote computer name.</param>
    internal WindowsServiceManager(
        IServiceNativeApi nativeApi,
        IWaitStrategy waitStrategy,
        IPlatform platform,
        string? machineName = null)
    {
        _nativeApi = Guard.NotNull(nativeApi, nameof(nativeApi));
        _waitStrategy = Guard.NotNull(waitStrategy, nameof(waitStrategy));
        _platform = Guard.NotNull(platform, nameof(platform));
        _machineName = string.IsNullOrWhiteSpace(machineName) ? null : machineName;
    }

    /// <inheritdoc/>
    public bool Exists(string serviceName)
    {
        ValidateServiceName(serviceName);
        EnsureWindows();
        using var manager = _nativeApi.OpenManager(_machineName, ScmAccessRights.Connect);
        using var service = _nativeApi.OpenService(manager, serviceName, ServiceAccessRights.QueryStatus);
        return service is not null;
    }

    /// <inheritdoc/>
    public void Create(ServiceDefinition definition)
    {
        definition = Guard.NotNull(definition, nameof(definition));

        ValidateServiceName(definition.ServiceName);
        ValidateRequired(definition.DisplayName, nameof(definition.DisplayName));
        ValidateRequired(definition.ExecutablePath, nameof(definition.ExecutablePath));
        ValidateStartMode(definition.StartMode);
        EnsureWindows();

        var imagePath = WindowsCommandLine.Build(definition.ExecutablePath, definition.Arguments);
        var dependencies = ServiceDependencies.ToMultiString(definition.Dependencies);
        using var manager = _nativeApi.OpenManager(
            _machineName,
            ScmAccessRights.Connect | ScmAccessRights.CreateService);
        using var service = _nativeApi.CreateService(manager, definition, imagePath, dependencies);
        if (definition.Description is not null)
        {
            _nativeApi.SetDescription(service, definition.ServiceName, definition.Description);
        }
    }

    /// <inheritdoc/>
    public void Configure(string serviceName, ServiceUpdate update)
    {
        ValidateServiceName(serviceName);
        update = Guard.NotNull(update, nameof(update));

        if (update.StartMode.HasValue)
        {
            ValidateStartMode(update.StartMode.Value);
        }

        if (update.ExecutablePath is not null)
        {
            ValidateRequired(update.ExecutablePath, nameof(update.ExecutablePath));
        }

        if (update.Arguments is not null && update.ExecutablePath is null)
        {
            throw new ArgumentException(
                "ExecutablePath is required when replacing service arguments.",
                nameof(update));
        }

        EnsureWindows();
        var imagePath = update.ExecutablePath is null
            ? null
            : WindowsCommandLine.Build(update.ExecutablePath, update.Arguments ?? Array.Empty<string>());
        var dependencies = ServiceDependencies.ToMultiString(update.Dependencies);
        using var manager = _nativeApi.OpenManager(_machineName, ScmAccessRights.Connect);
        using var service = OpenRequiredService(manager, serviceName, ServiceAccessRights.ChangeConfig);
        _nativeApi.ChangeConfiguration(service, serviceName, update, imagePath, dependencies);
        if (update.ChangeDescription)
        {
            _nativeApi.SetDescription(service, serviceName, update.Description);
        }
    }

    /// <inheritdoc/>
    public ServiceSnapshot Query(string serviceName)
    {
        ValidateServiceName(serviceName);
        EnsureWindows();
        using var manager = _nativeApi.OpenManager(_machineName, ScmAccessRights.Connect);
        using var service = OpenRequiredService(
            manager,
            serviceName,
            ServiceAccessRights.QueryStatus | ServiceAccessRights.QueryConfig);
        var configuration = _nativeApi.QueryConfiguration(service, serviceName);
        var status = _nativeApi.QueryStatus(service, serviceName);
        return new(configuration, status);
    }

    /// <inheritdoc/>
    public ServiceStatus Start(string serviceName, TimeSpan timeout, params string[] arguments)
    {
        ValidateServiceName(serviceName);
        ValidateTimeout(timeout);
        EnsureWindows();
        using var manager = _nativeApi.OpenManager(_machineName, ScmAccessRights.Connect);
        using var service = OpenRequiredService(
            manager,
            serviceName,
            ServiceAccessRights.QueryStatus | ServiceAccessRights.Start | ServiceAccessRights.PauseContinue);
        var status = _nativeApi.QueryStatus(service, serviceName);
        if (status.State == ServiceState.Running)
        {
            return status;
        }

        if (status.State == ServiceState.StartPending)
        {
            return WaitForState(service, serviceName, ServiceState.StartPending, ServiceState.Running, timeout);
        }

        if (status.State == ServiceState.Paused)
        {
            _nativeApi.Control(service, serviceName, ServiceControl.Continue);
            return WaitForState(service, serviceName, ServiceState.ContinuePending, ServiceState.Running, timeout);
        }

        if (status.State != ServiceState.Stopped)
        {
            throw InvalidTransition(serviceName, status.State, ServiceState.Running);
        }

        _nativeApi.Start(service, serviceName, arguments ?? Array.Empty<string>());
        return WaitForState(service, serviceName, ServiceState.StartPending, ServiceState.Running, timeout);
    }

    /// <inheritdoc/>
    public ServiceStatus Stop(string serviceName, TimeSpan timeout) =>
        Control(serviceName, timeout, ServiceControl.Stop, ServiceState.StopPending, ServiceState.Stopped);

    /// <inheritdoc/>
    public ServiceStatus Pause(string serviceName, TimeSpan timeout) =>
        Control(serviceName, timeout, ServiceControl.Pause, ServiceState.PausePending, ServiceState.Paused);

    /// <inheritdoc/>
    public ServiceStatus Continue(string serviceName, TimeSpan timeout) =>
        Control(serviceName, timeout, ServiceControl.Continue, ServiceState.ContinuePending, ServiceState.Running);

    /// <inheritdoc/>
    public bool Delete(string serviceName, TimeSpan timeout)
    {
        ValidateServiceName(serviceName);
        ValidateTimeout(timeout);
        EnsureWindows();
        using var manager = _nativeApi.OpenManager(_machineName, ScmAccessRights.Connect);
        using var service = _nativeApi.OpenService(
            manager,
            serviceName,
            ServiceAccessRights.QueryStatus | ServiceAccessRights.Stop | ServiceAccessRights.Delete);
        if (service is null)
        {
            return false;
        }

        var status = _nativeApi.QueryStatus(service, serviceName);
        if (status.State == ServiceState.StopPending)
        {
            _ = WaitForState(service, serviceName, ServiceState.StopPending, ServiceState.Stopped, timeout);
        }
        else if (status.State != ServiceState.Stopped)
        {
            _nativeApi.Control(service, serviceName, ServiceControl.Stop);
            _ = WaitForState(service, serviceName, ServiceState.StopPending, ServiceState.Stopped, timeout);
        }

        _nativeApi.Delete(service, serviceName);
        return true;
    }

    /// <summary>Gets the default lifecycle transition timeout.</summary>
    /// <returns>The default timeout.</returns>
    internal static TimeSpan GetDefaultTimeout() => DefaultTimeout;

    /// <summary>Calculates a bounded service-status polling interval.</summary>
    /// <param name="waitHintMilliseconds">The native wait hint.</param>
    /// <param name="remainingMilliseconds">The remaining timeout duration.</param>
    /// <returns>The next delay in milliseconds.</returns>
    private static int GetPollDelay(uint waitHintMilliseconds, long remainingMilliseconds)
    {
        var suggested = Math.Max(
            MinimumPollMilliseconds,
            Math.Min(MaximumPollMilliseconds, waitHintMilliseconds / WaitHintDivisor));
        return (int)Math.Min(suggested, remainingMilliseconds);
    }

    /// <summary>Determines whether a control is valid in the current state.</summary>
    /// <param name="control">The requested control.</param>
    /// <param name="state">The current state.</param>
    /// <returns>True when the control can be sent.</returns>
    private static bool CanControl(ServiceControl control, ServiceState state) => control switch
    {
        ServiceControl.Stop => state is ServiceState.Running or ServiceState.Paused,
        ServiceControl.Pause => state == ServiceState.Running,
        _ => state == ServiceState.Paused,
    };

    /// <summary>Maps a control message to the required service access.</summary>
    /// <param name="control">The requested control.</param>
    /// <returns>The required access flag.</returns>
    private static ServiceAccessRights GetControlAccess(ServiceControl control) =>
        control == ServiceControl.Stop ? ServiceAccessRights.Stop : ServiceAccessRights.PauseContinue;

    /// <summary>Creates a state-transition exception.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="currentState">The current state.</param>
    /// <param name="desiredState">The desired state.</param>
    /// <returns>The transition exception.</returns>
    private static InvalidOperationException InvalidTransition(
        string serviceName,
        ServiceState currentState,
        ServiceState desiredState) =>
        new($"Service '{serviceName}' cannot transition from {currentState} to {desiredState}.");

    /// <summary>Validates a stable service name.</summary>
    /// <param name="serviceName">The service name.</param>
    private static void ValidateServiceName(string serviceName) =>
        ValidateRequired(serviceName, nameof(serviceName));

    /// <summary>Validates required text.</summary>
    /// <param name="value">The text value.</param>
    /// <param name="parameterName">The caller parameter name.</param>
    private static void ValidateRequired(string value, string parameterName) =>
        _ = Guard.NotNullOrWhiteSpace(value, parameterName);

    /// <summary>Validates a lifecycle timeout.</summary>
    /// <param name="timeout">The timeout value.</param>
    private static void ValidateTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout must be positive and supported.");
        }
    }

    /// <summary>Validates a supported service start mode.</summary>
    /// <param name="startMode">The start mode.</param>
    private static void ValidateStartMode(ServiceStartMode startMode)
    {
        if (startMode is not ServiceStartMode.Automatic and not ServiceStartMode.Manual and not ServiceStartMode.Disabled)
        {
            throw new ArgumentOutOfRangeException(nameof(startMode), startMode, "The start mode is not supported.");
        }
    }

    /// <summary>Sends a lifecycle control message and waits for the desired state.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="timeout">The transition timeout.</param>
    /// <param name="control">The native control message.</param>
    /// <param name="pendingState">The expected pending state.</param>
    /// <param name="desiredState">The desired final state.</param>
    /// <returns>The final status.</returns>
    private ServiceStatus Control(
        string serviceName,
        TimeSpan timeout,
        ServiceControl control,
        ServiceState pendingState,
        ServiceState desiredState)
    {
        ValidateServiceName(serviceName);
        ValidateTimeout(timeout);
        EnsureWindows();
        using var manager = _nativeApi.OpenManager(_machineName, ScmAccessRights.Connect);
        using var service = OpenRequiredService(
            manager,
            serviceName,
            ServiceAccessRights.QueryStatus | GetControlAccess(control));
        var status = _nativeApi.QueryStatus(service, serviceName);
        if (status.State == desiredState)
        {
            return status;
        }

        if (status.State == pendingState)
        {
            return WaitForState(service, serviceName, pendingState, desiredState, timeout);
        }

        if (!CanControl(control, status.State))
        {
            throw InvalidTransition(serviceName, status.State, desiredState);
        }

        _nativeApi.Control(service, serviceName, control);
        return WaitForState(service, serviceName, pendingState, desiredState, timeout);
    }

    /// <summary>Waits for a pending service transition to complete.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="pendingState">The expected pending state.</param>
    /// <param name="desiredState">The desired final state.</param>
    /// <param name="timeout">The maximum wait duration.</param>
    /// <returns>The final status.</returns>
    private ServiceStatus WaitForState(
        IServiceHandle service,
        string serviceName,
        ServiceState pendingState,
        ServiceState desiredState,
        TimeSpan timeout)
    {
        var deadline = _waitStrategy.ElapsedMilliseconds + (long)timeout.TotalMilliseconds;
        while (true)
        {
            var status = _nativeApi.QueryStatus(service, serviceName);
            if (status.State == desiredState)
            {
                return status;
            }

            if (status.State != pendingState)
            {
                throw InvalidTransition(serviceName, status.State, desiredState);
            }

            var remaining = deadline - _waitStrategy.ElapsedMilliseconds;
            if (remaining <= 0)
            {
                throw new TimeoutException($"Service '{serviceName}' did not reach {desiredState} within {timeout}.");
            }

            _waitStrategy.Delay(GetPollDelay(status.WaitHintMilliseconds, remaining));
        }
    }

    /// <summary>Opens an installed service or throws a missing-service exception.</summary>
    /// <param name="manager">The manager handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="access">The required service access.</param>
    /// <returns>The owned service handle.</returns>
    private IServiceHandle OpenRequiredService(
        IServiceHandle manager,
        string serviceName,
        ServiceAccessRights access) =>
        _nativeApi.OpenService(manager, serviceName, access)
        ?? throw new ServiceOperationException(
            "OpenService",
            serviceName,
            MissingServiceError,
            $"Service '{serviceName}' is not installed.");

    /// <summary>Ensures the native API is invoked only on Windows.</summary>
    private void EnsureWindows()
    {
        if (!_platform.IsWindows)
        {
            throw new PlatformNotSupportedException("Windows service management is available only on Windows.");
        }
    }
}
