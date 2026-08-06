// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ServiceInstaller.Enums;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace ServiceInstaller;

/// <summary>Declares the native Windows Service Control Manager entry points and layouts.</summary>
[ExcludeFromCodeCoverage]
#if NET7_0_OR_GREATER
internal static partial class NativeMethods
#else
internal static class NativeMethods
#endif
{
    /// <summary>The byte size of the native SERVICE_STATUS structure.</summary>
    internal const int BasicServiceStatusSize = 28;

#if NET7_0_OR_GREATER
    /// <summary>Opens the Service Control Manager.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "OpenSCManagerW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial NativeServiceHandle OpenSCManager(
        string? machineName,
        string? databaseName,
        ScmAccessRights desiredAccess);

    /// <summary>Opens an installed service.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial NativeServiceHandle OpenService(
        NativeServiceHandle serviceControlManager,
        string serviceName,
        ServiceAccessRights desiredAccess);

    /// <summary>Creates a service.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "CreateServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial NativeServiceHandle CreateService(
        NativeServiceHandle serviceControlManager,
        string serviceName,
        string displayName,
        ServiceAccessRights desiredAccess,
        uint serviceType,
        uint startType,
        uint errorControl,
        string binaryPathName,
        string? loadOrderGroup,
        IntPtr tagId,
        string? dependencies,
        string? serviceStartName,
        string? password);

    /// <summary>Closes an owned service handle.</summary>
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseServiceHandle(IntPtr serviceObject);

    /// <summary>Queries extended service status.</summary>
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceStatusEx(
        NativeServiceHandle service,
        int infoLevel,
        IntPtr buffer,
        int bufferSize,
        out int bytesNeeded);

    /// <summary>Queries base service configuration.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "QueryServiceConfigW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceConfig(
        NativeServiceHandle service,
        IntPtr serviceConfig,
        uint bufferSize,
        out uint bytesNeeded);

    /// <summary>Queries extended service configuration.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "QueryServiceConfig2W", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceConfig2(
        NativeServiceHandle service,
        int infoLevel,
        IntPtr buffer,
        uint bufferSize,
        out uint bytesNeeded);

    /// <summary>Changes base service configuration.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfigW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ChangeServiceConfig(
        NativeServiceHandle service,
        uint serviceType,
        uint startType,
        uint errorControl,
        string? binaryPathName,
        string? loadOrderGroup,
        IntPtr tagId,
        string? dependencies,
        string? serviceStartName,
        string? password,
        string? displayName);

    /// <summary>Changes extended service configuration.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2W", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ChangeServiceConfig2(
        NativeServiceHandle service,
        int infoLevel,
        IntPtr info);

    /// <summary>Starts a service.</summary>
    [LibraryImport("advapi32.dll", EntryPoint = "StartServiceW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool StartService(
        NativeServiceHandle service,
        int argumentCount,
        IntPtr arguments);

    /// <summary>Sends a control message to a service.</summary>
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ControlService(
        NativeServiceHandle service,
        ServiceControl control,
        IntPtr status);

    /// <summary>Marks a service for deletion.</summary>
    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteService(NativeServiceHandle service);
#else
    /// <summary>Opens the Service Control Manager.</summary>
    /// <param name="machineName">The optional remote computer name.</param>
    /// <param name="databaseName">The optional services database name.</param>
    /// <param name="desiredAccess">The requested manager access.</param>
    /// <returns>An owned manager handle.</returns>
    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern NativeServiceHandle OpenSCManager(
        string? machineName,
        string? databaseName,
        ScmAccessRights desiredAccess);

    /// <summary>Opens an installed service.</summary>
    /// <param name="serviceControlManager">The manager handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="desiredAccess">The requested service access.</param>
    /// <returns>An owned service handle.</returns>
    [DllImport("advapi32.dll", EntryPoint = "OpenServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern NativeServiceHandle OpenService(
        NativeServiceHandle serviceControlManager,
        string serviceName,
        ServiceAccessRights desiredAccess);

    /// <summary>Creates a service.</summary>
    /// <param name="serviceControlManager">The manager handle.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="displayName">The service display name.</param>
    /// <param name="desiredAccess">The requested service access.</param>
    /// <param name="serviceType">The native service type.</param>
    /// <param name="startType">The native start type.</param>
    /// <param name="errorControl">The native error-control value.</param>
    /// <param name="binaryPathName">The escaped image path.</param>
    /// <param name="loadOrderGroup">The optional load-order group.</param>
    /// <param name="tagId">The optional tag pointer.</param>
    /// <param name="dependencies">The dependency multi-string.</param>
    /// <param name="serviceStartName">The optional service account.</param>
    /// <param name="password">The optional account password.</param>
    /// <returns>An owned service handle.</returns>
    [DllImport("advapi32.dll", EntryPoint = "CreateServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern NativeServiceHandle CreateService(
        NativeServiceHandle serviceControlManager,
        string serviceName,
        string displayName,
        ServiceAccessRights desiredAccess,
        uint serviceType,
        uint startType,
        uint errorControl,
        string binaryPathName,
        string? loadOrderGroup,
        IntPtr tagId,
        string? dependencies,
        string? serviceStartName,
        string? password);

    /// <summary>Closes an owned service handle.</summary>
    /// <param name="serviceObject">The raw handle value.</param>
    /// <returns>True when the handle was closed.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseServiceHandle(IntPtr serviceObject);

    /// <summary>Queries extended service status.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="infoLevel">The status information level.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="bytesNeeded">The required bytes.</param>
    /// <returns>True when the query succeeded.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryServiceStatusEx(
        NativeServiceHandle service,
        int infoLevel,
        IntPtr buffer,
        int bufferSize,
        out int bytesNeeded);

    /// <summary>Queries base service configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceConfig">The destination buffer.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="bytesNeeded">The required bytes.</param>
    /// <returns>True when the query succeeded.</returns>
    [DllImport("advapi32.dll", EntryPoint = "QueryServiceConfigW", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryServiceConfig(
        NativeServiceHandle service,
        IntPtr serviceConfig,
        uint bufferSize,
        out uint bytesNeeded);

    /// <summary>Queries extended service configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="infoLevel">The configuration information level.</param>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="bufferSize">The buffer size.</param>
    /// <param name="bytesNeeded">The required bytes.</param>
    /// <returns>True when the query succeeded.</returns>
    [DllImport("advapi32.dll", EntryPoint = "QueryServiceConfig2W", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryServiceConfig2(
        NativeServiceHandle service,
        int infoLevel,
        IntPtr buffer,
        uint bufferSize,
        out uint bytesNeeded);

    /// <summary>Changes base service configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="serviceType">The native service type.</param>
    /// <param name="startType">The native start type.</param>
    /// <param name="errorControl">The native error-control value.</param>
    /// <param name="binaryPathName">The optional escaped image path.</param>
    /// <param name="loadOrderGroup">The optional load-order group.</param>
    /// <param name="tagId">The optional tag pointer.</param>
    /// <param name="dependencies">The optional dependency multi-string.</param>
    /// <param name="serviceStartName">The optional service account.</param>
    /// <param name="password">The optional account password.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>True when the update succeeded.</returns>
    [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfigW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ChangeServiceConfig(
        NativeServiceHandle service,
        uint serviceType,
        uint startType,
        uint errorControl,
        string? binaryPathName,
        string? loadOrderGroup,
        IntPtr tagId,
        string? dependencies,
        string? serviceStartName,
        string? password,
        string? displayName);

    /// <summary>Changes extended service configuration.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="infoLevel">The configuration information level.</param>
    /// <param name="info">The configuration buffer.</param>
    /// <returns>True when the update succeeded.</returns>
    [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2W", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ChangeServiceConfig2(
        NativeServiceHandle service,
        int infoLevel,
        IntPtr info);

    /// <summary>Starts a service.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="argumentCount">The argument count.</param>
    /// <param name="arguments">The argument pointer array.</param>
    /// <returns>True when the request succeeded.</returns>
    [DllImport("advapi32.dll", EntryPoint = "StartServiceW", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool StartService(
        NativeServiceHandle service,
        int argumentCount,
        IntPtr arguments);

    /// <summary>Sends a control message to a service.</summary>
    /// <param name="service">The service handle.</param>
    /// <param name="control">The control message.</param>
    /// <param name="status">The destination status buffer.</param>
    /// <returns>True when the request succeeded.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ControlService(
        NativeServiceHandle service,
        ServiceControl control,
        IntPtr status);

    /// <summary>Marks a service for deletion.</summary>
    /// <param name="service">The service handle.</param>
    /// <returns>True when the request succeeded.</returns>
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteService(NativeServiceHandle service);
#endif

    /// <summary>Defines the extended native service-status buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class ServiceStatusProcess
    {
        /// <summary>Gets the native service type.</summary>
        internal uint ServiceType { get; }

        /// <summary>Gets the current service state.</summary>
        internal ServiceState CurrentState { get; }

        /// <summary>Gets the controls accepted by the service.</summary>
        internal ServiceAcceptedControls ControlsAccepted { get; }

        /// <summary>Gets the pending-operation checkpoint.</summary>
        internal uint CheckPoint { get; }

        /// <summary>Gets the pending-operation wait hint.</summary>
        internal uint WaitHint { get; }

        /// <summary>Gets the service process identifier.</summary>
        internal uint ProcessId { get; }

        /// <summary>Gets the Win32 exit code.</summary>
        internal uint Win32ExitCode { get; }

        /// <summary>Gets the service-specific exit code.</summary>
        internal uint ServiceSpecificExitCode { get; }

        /// <summary>Gets the service flags.</summary>
        internal uint ServiceFlags { get; }
    }

    /// <summary>Defines the native service configuration buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class QueryServiceConfigData
    {
        /// <summary>Gets the native service type.</summary>
        internal uint ServiceType { get; }

        /// <summary>Gets the configured start type.</summary>
        internal uint StartType { get; }

        /// <summary>Gets the binary command-line pointer.</summary>
        internal IntPtr BinaryPathName { get; }

        /// <summary>Gets the service-account pointer.</summary>
        internal IntPtr ServiceStartName { get; }

        /// <summary>Gets the display-name pointer.</summary>
        internal IntPtr DisplayName { get; }

        /// <summary>Gets the configured error-control value.</summary>
        internal uint ErrorControl { get; }

        /// <summary>Gets the load-order group pointer.</summary>
        internal IntPtr LoadOrderGroup { get; }

        /// <summary>Gets the service tag identifier.</summary>
        internal uint TagId { get; }

        /// <summary>Gets the dependencies pointer.</summary>
        internal IntPtr Dependencies { get; }
    }

    /// <summary>Defines the native service-description buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class ServiceDescription
    {
        /// <summary>Initializes a new instance of the <see cref="ServiceDescription"/> class.</summary>
        /// <param name="description">The native description pointer.</param>
        internal ServiceDescription(IntPtr description) => Description = description;

        /// <summary>Gets the description pointer.</summary>
        internal IntPtr Description { get; }
    }
}
