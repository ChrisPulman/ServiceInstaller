// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ServiceInstaller.Enums;
using ServiceInstaller.Internal;

namespace ServiceInstaller;

/// <summary>Implements the native Windows Service Control Manager boundary.</summary>
[ExcludeFromCodeCoverage]
internal sealed class NativeServiceApi : IServiceNativeApi
{
    /// <summary>The Win32 insufficient-buffer error.</summary>
    private const int ErrorInsufficientBuffer = 122;

    /// <summary>The Win32 missing-service error.</summary>
    private const int ErrorServiceDoesNotExist = 1060;

    /// <summary>The native no-change configuration value.</summary>
    private const uint ServiceNoChange = uint.MaxValue;

    /// <summary>The native own-process service type.</summary>
    private const uint ServiceWin32OwnProcess = 0x00000010;

    /// <summary>The normal service error-control value.</summary>
    private const uint ServiceErrorNormal = 0x00000001;

    /// <summary>The service-description configuration level.</summary>
    private const int ServiceConfigDescription = 1;

    /// <summary>The extended service-status information level.</summary>
    private const int ServiceStatusProcessInfo = 0;

    /// <inheritdoc/>
    public IServiceHandle OpenManager(string? machineName, ScmAccessRights access)
    {
        var handle = NativeMethods.OpenSCManager(machineName, null, access);
        return handle.IsInvalid ? throw Failure("OpenSCManager", null) : handle;
    }

    /// <inheritdoc/>
    public IServiceHandle? OpenService(
        IServiceHandle manager,
        string serviceName,
        ServiceAccessRights access)
    {
        var handle = NativeMethods.OpenService(GetHandle(manager), serviceName, access);
        if (!handle.IsInvalid)
        {
            return handle;
        }

        var error = Marshal.GetLastWin32Error();
        handle.Dispose();
        return error == ErrorServiceDoesNotExist
            ? null
            : throw Failure("OpenService", serviceName, error);
    }

    /// <inheritdoc/>
    public IServiceHandle CreateService(
        IServiceHandle manager,
        ServiceDefinition definition,
        string imagePath,
        string? dependencies)
    {
        var handle = NativeMethods.CreateService(
            GetHandle(manager),
            definition.ServiceName,
            definition.DisplayName,
            ServiceAccessRights.QueryConfig
                | ServiceAccessRights.ChangeConfig
                | ServiceAccessRights.QueryStatus
                | ServiceAccessRights.Start
                | ServiceAccessRights.Stop
                | ServiceAccessRights.PauseContinue
                | ServiceAccessRights.Delete,
            ServiceWin32OwnProcess,
            (uint)definition.StartMode,
            ServiceErrorNormal,
            imagePath,
            null,
            IntPtr.Zero,
            dependencies,
            definition.AccountName,
            definition.Password);
        return handle.IsInvalid ? throw Failure("CreateService", definition.ServiceName) : handle;
    }

    /// <inheritdoc/>
    public ServiceStatus QueryStatus(IServiceHandle service, string serviceName)
    {
        var size = Marshal.SizeOf<NativeMethods.ServiceStatusProcess>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!NativeMethods.QueryServiceStatusEx(
                    GetHandle(service),
                    ServiceStatusProcessInfo,
                    buffer,
                    size,
                    out _))
            {
                throw Failure("QueryServiceStatusEx", serviceName);
            }

            var status = Marshal.PtrToStructure<NativeMethods.ServiceStatusProcess>(buffer)!;
            return new(
                status.CurrentState,
                status.ControlsAccepted,
                status.ProcessId,
                status.CheckPoint,
                status.WaitHint);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc/>
    public ServiceConfiguration QueryConfiguration(IServiceHandle service, string serviceName)
    {
        var handle = GetHandle(service);
        _ = NativeMethods.QueryServiceConfig(handle, IntPtr.Zero, 0, out var requiredSize);
        var error = Marshal.GetLastWin32Error();
        if (error != ErrorInsufficientBuffer || requiredSize == 0)
        {
            throw Failure("QueryServiceConfig", serviceName, error);
        }

        var buffer = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            if (!NativeMethods.QueryServiceConfig(handle, buffer, requiredSize, out _))
            {
                throw Failure("QueryServiceConfig", serviceName);
            }

            var configuration = Marshal.PtrToStructure<NativeMethods.QueryServiceConfigData>(buffer)!;
            return new(
                serviceName,
                Marshal.PtrToStringUni(configuration.DisplayName) ?? serviceName,
                Marshal.PtrToStringUni(configuration.BinaryPathName) ?? string.Empty,
                (ServiceStartMode)configuration.StartType,
                Marshal.PtrToStringUni(configuration.ServiceStartName) ?? string.Empty,
                QueryDescription(service, serviceName));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc/>
    public void ChangeConfiguration(
        IServiceHandle service,
        string serviceName,
        ServiceUpdate update,
        string? imagePath,
        string? dependencies)
    {
        var accountName = update.ChangeAccount ? update.AccountName ?? "LocalSystem" : null;
        if (!NativeMethods.ChangeServiceConfig(
                GetHandle(service),
                ServiceNoChange,
                update.StartMode.HasValue ? (uint)update.StartMode.Value : ServiceNoChange,
                ServiceNoChange,
                imagePath,
                null,
                IntPtr.Zero,
                dependencies,
                accountName,
                update.ChangeAccount ? update.Password : null,
                update.DisplayName))
        {
            throw Failure("ChangeServiceConfig", serviceName);
        }
    }

    /// <inheritdoc/>
    public void SetDescription(IServiceHandle service, string serviceName, string? description)
    {
        var descriptionPointer = description is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(description);
        var structurePointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.ServiceDescription>());
        try
        {
            Marshal.StructureToPtr(new NativeMethods.ServiceDescription(descriptionPointer), structurePointer, false);
            if (!NativeMethods.ChangeServiceConfig2(
                    GetHandle(service),
                    ServiceConfigDescription,
                    structurePointer))
            {
                throw Failure("ChangeServiceConfig2", serviceName);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(structurePointer);
            if (descriptionPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(descriptionPointer);
            }
        }
    }

    /// <inheritdoc/>
    public void Start(IServiceHandle service, string serviceName, IReadOnlyList<string> arguments)
    {
        var argumentPointers = new IntPtr[arguments.Count];
        var arrayPointer = arguments.Count == 0
            ? IntPtr.Zero
            : Marshal.AllocHGlobal(IntPtr.Size * arguments.Count);
        try
        {
            for (var index = 0; index < arguments.Count; index++)
            {
                argumentPointers[index] = Marshal.StringToHGlobalUni(arguments[index]);
                Marshal.WriteIntPtr(arrayPointer, index * IntPtr.Size, argumentPointers[index]);
            }

            if (!NativeMethods.StartService(GetHandle(service), arguments.Count, arrayPointer))
            {
                throw Failure("StartService", serviceName);
            }
        }
        finally
        {
            foreach (var argumentPointer in argumentPointers)
            {
                if (argumentPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(argumentPointer);
                }
            }

            if (arrayPointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(arrayPointer);
            }
        }
    }

    /// <inheritdoc/>
    public void Control(IServiceHandle service, string serviceName, ServiceControl control)
    {
        var buffer = Marshal.AllocHGlobal(NativeMethods.BasicServiceStatusSize);
        try
        {
            if (!NativeMethods.ControlService(GetHandle(service), control, buffer))
            {
                throw Failure("ControlService", serviceName);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <inheritdoc/>
    public void Delete(IServiceHandle service, string serviceName)
    {
        if (!NativeMethods.DeleteService(GetHandle(service)))
        {
            throw Failure("DeleteService", serviceName);
        }
    }

    /// <summary>Returns the strongly owned native handle for a P/Invoke call.</summary>
    /// <param name="service">The abstract service handle.</param>
    /// <returns>The native safe handle.</returns>
    private static NativeServiceHandle GetHandle(IServiceHandle service) => (NativeServiceHandle)service;

    /// <summary>Queries the optional service description.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <returns>The optional description.</returns>
    private static string? QueryDescription(IServiceHandle service, string serviceName)
    {
        var handle = GetHandle(service);
        _ = NativeMethods.QueryServiceConfig2(
            handle,
            ServiceConfigDescription,
            IntPtr.Zero,
            0,
            out var requiredSize);
        var error = Marshal.GetLastWin32Error();
        if (error != ErrorInsufficientBuffer || requiredSize == 0)
        {
            throw Failure("QueryServiceConfig2", serviceName, error);
        }

        var buffer = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            if (!NativeMethods.QueryServiceConfig2(
                    handle,
                    ServiceConfigDescription,
                    buffer,
                    requiredSize,
                    out _))
            {
                throw Failure("QueryServiceConfig2", serviceName);
            }

            var description = Marshal.PtrToStructure<NativeMethods.ServiceDescription>(buffer)!;
            return Marshal.PtrToStringUni(description.Description);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Creates a safe service-operation exception from the latest Win32 error.</summary>
    /// <param name="operation">The failed operation.</param>
    /// <param name="serviceName">The optional service name.</param>
    /// <param name="errorCode">An optional captured Win32 error.</param>
    /// <returns>The safe operation exception.</returns>
    private static ServiceOperationException Failure(
        string operation,
        string? serviceName,
        int? errorCode = null)
    {
        var error = errorCode ?? Marshal.GetLastWin32Error();
        var target = serviceName is null ? "the Service Control Manager" : $"service '{serviceName}'";
        return new(
            operation,
            serviceName,
            error,
            $"{operation} failed for {target}: {new Win32Exception(error).Message} (Win32 {error}).");
    }
}
