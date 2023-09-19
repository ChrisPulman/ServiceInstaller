// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using ServiceInstaller.Enums;

namespace ServiceInstaller;

/// <summary>
/// Service Controller.
/// </summary>
public static class ServiceController
{
    private static string[] _commands = Array.Empty<string>();

    /// <summary>
    /// Adds the commands.
    /// </summary>
    /// <param name="commands">The commands.</param>
    public static void AddApplicationArguments(params string[] commands) => _commands = commands;

    /// <summary>
    /// Handles the service request.
    /// </summary>
    /// <param name="command">The command (-Install, -Uninstall, -Start, -Stop, -Status, -IsInstalled) normally pass args[0] from commandline variables.</param>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="displayName">The display name of the service.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns>
    /// The responce for the request.
    /// </returns>
    public static string HandleRequest(string? command, string serviceName, string displayName, params string[] parameters)
    {
        try
        {
            var status = GetServiceStatus(serviceName);
            switch (command?.ToLowerInvariant())
            {
                case "-install":
                    // Installs and starts the service
                    if (status == ServiceState.NotFound)
                    {
                        var fileName = GetServicePath();
                        if (fileName != null)
                        {
                            InstallAndStart(serviceName, displayName, fileName, parameters ?? Array.Empty<string>());
                            Thread.Sleep(1000);
                            status = GetServiceStatus(serviceName);
                            if (status == ServiceState.Running)
                            {
                                return "Service installed and started successfully";
                            }

                            return "Service installation failed";
                        }
                    }
                    else if (status != ServiceState.Running)
                    {
                        StartService(serviceName);
                        Thread.Sleep(1000);
                        status = GetServiceStatus(serviceName);
                        if (status == ServiceState.Running)
                        {
                            return "Service started successfully";
                        }

                        return "Service failed to start";
                    }
                    else if (status == ServiceState.Running)
                    {
                        return "Service started successfully";
                    }

                    return "Service installation failed";
                case "-uninstall":
                    // Uninstalls the service
                    Uninstall(serviceName);
                    return "Service Uninstalled, please Reboot";
                case "-start":
                    // Starts the service
                    StartService(serviceName);
                    Thread.Sleep(1000);
                    status = GetServiceStatus(serviceName);
                    if (status == ServiceState.Running)
                    {
                        return "Service started successfully";
                    }

                    return "Service failed to start";

                case "-stop":
                    // Stops the service
                    StopService(serviceName);
                    Thread.Sleep(1000);
                    status = GetServiceStatus(serviceName);
                    if (status == ServiceState.Stopped)
                    {
                        return "Service stopped successfully";
                    }

                    return "Service failed to stop";

                case "-status":
                    // Checks the status of the service
                    return GetServiceStatus(serviceName).ToString();
                case "-isinstalled":
                    // Check if service is installed
                    return $"The service {(ServiceIsInstalled(serviceName) ? "is" : "is not")} installed";

                default:
                    var sb = new StringBuilder()
                        .AppendLine("Valid Service Arguments are:")
                        .AppendLine("-Install")
                        .AppendLine("-Uninstall")
                        .AppendLine("-Start")
                        .AppendLine("-Stop")
                        .AppendLine("-Status")
                        .AppendLine("-IsInstalled");

                    if (_commands.Length > 0)
                    {
                        sb.AppendLine("Additional Arguments:");
                        foreach (var item in _commands)
                        {
                            sb.AppendLine(item);
                        }
                    }

                    return sb.ToString();
            }
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    /// <summary>
    /// Uninstalls the specified service name.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <exception cref="ApplicationException">
    /// Service not installed.
    /// or
    /// Could not delete service " + Marshal.GetLastWin32Error().
    /// </exception>
    internal static void Uninstall(string serviceName)
    {
        var scm = OpenSCManager(ScmAccessRights.AllAccess);

        try
        {
            var service = NativeMethods.OpenService(scm, serviceName, ServiceAccessRights.AllAccess);
            if (service == IntPtr.Zero)
            {
                throw new ApplicationException("Service not installed.");
            }

            try
            {
                StopService(service);
                if (!NativeMethods.DeleteService(service))
                {
                    throw new ApplicationException("Could not delete service " + Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    /// <summary>
    /// Services the is installed.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <returns>[True] if service is installed.</returns>
    internal static bool ServiceIsInstalled(string serviceName)
    {
        var scm = OpenSCManager(ScmAccessRights.Connect);

        try
        {
            var service = NativeMethods.OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);

            if (service == IntPtr.Zero)
            {
                return false;
            }

            NativeMethods.CloseServiceHandle(service);
            return true;
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    /// <summary>
    /// Installs the and start.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="parameters">The parameters.</param>
    /// <exception cref="System.ApplicationException">Failed to install service.</exception>
    internal static void InstallAndStart(string serviceName, string displayName, string fileName, params string[] parameters)
    {
        var scm = OpenSCManager(ScmAccessRights.AllAccess);

        try
        {
            var service = NativeMethods.OpenService(scm, serviceName, ServiceAccessRights.AllAccess);

            if (service == IntPtr.Zero)
            {
                var fileNameAndParameters = CombineFileNameAndParameters(fileName, parameters);
                service = NativeMethods.CreateService(scm, serviceName, displayName, ServiceAccessRights.AllAccess, NativeMethods.SERVICE_WIN32_OWN_PROCESS, ServiceBootFlag.AutoStart, ServiceError.Normal, fileNameAndParameters, null!, IntPtr.Zero, null!, null!, null!);
            }

            if (service == IntPtr.Zero)
            {
                throw new ApplicationException("Failed to install service.");
            }

            try
            {
                StartService(service);
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    /// <summary>
    /// Starts the service.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <exception cref="System.ApplicationException">Could not open service.</exception>
    internal static void StartService(string serviceName)
    {
        var scm = OpenSCManager(ScmAccessRights.Connect);

        try
        {
            var service = NativeMethods.OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Start);
            if (service == IntPtr.Zero)
            {
                throw new ApplicationException("Could not open service.");
            }

            try
            {
                StartService(service);
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <exception cref="ApplicationException">Could not open service.</exception>
    internal static void StopService(string serviceName)
    {
        var scm = OpenSCManager(ScmAccessRights.Connect);

        try
        {
            var service = NativeMethods.OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Stop);
            if (service == IntPtr.Zero)
            {
                throw new ApplicationException("Could not open service.");
            }

            try
            {
                StopService(service);
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    /// <summary>
    /// Gets the service path.
    /// </summary>
    /// <param name="processName">Name of the process.</param>
    /// <returns>A string of the path to the service.</returns>
    internal static string? GetServicePath(string processName)
    {
        // Ensure the process is running in Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return string.Empty;
        }

        var process = Process.GetProcessesByName(processName).FirstOrDefault();
        try
        {
            return process?.MainModule?.FileName;
        }
        catch
        {
            GetExecutablePath(process?.Id.ToString());
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the service path.
    /// </summary>
    /// <returns>A string of the path to the service.</returns>
    internal static string? GetServicePath()
    {
        var process = Process.GetCurrentProcess();
        try
        {
            return process?.MainModule?.FileName;
        }
        catch
        {
            GetExecutablePath(process?.Id.ToString());
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets the service status.
    /// </summary>
    /// <param name="serviceName">Name of the service.</param>
    /// <returns>The current state of the service.</returns>
    internal static ServiceState GetServiceStatus(string serviceName)
    {
        var scm = OpenSCManager(ScmAccessRights.Connect);

        try
        {
            var service = NativeMethods.OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);
            if (service == IntPtr.Zero)
            {
                return ServiceState.NotFound;
            }

            try
            {
                return GetServiceStatus(service);
            }
            finally
            {
                NativeMethods.CloseServiceHandle(service);
            }
        }
        finally
        {
            NativeMethods.CloseServiceHandle(scm);
        }
    }

    private static string CombineFileNameAndParameters(string fileName, string[] parameters)
    {
        if (parameters.Length == 0)
        {
            return fileName;
        }

        var sb = new StringBuilder();
        sb.Append('"').Append(fileName).Append('"').Append(' ');

        for (var i = 0; i < parameters.Length; i++)
        {
            sb.Append(parameters[i]).Append(' ');
        }

        // remove space from last parameter
        sb.Length--;

        return sb.ToString();
    }

    private static string? GetExecutablePath(string? processId)
    {
        // Ensure the process is running in Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return string.Empty;
        }

        const string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
        var searcher = new ManagementObjectSearcher(query);
#pragma warning disable CA1416 // Validate platform compatibility
        foreach (var path in from item in searcher.Get().Cast<ManagementObject>()
                             let id = item["ProcessID"]
                             let path = item["ExecutablePath"]
                             where path != null && id.ToString() == processId
                             select path)
        {
            return path.ToString();
        }
#pragma warning restore CA1416 // Validate platform compatibility

        return string.Empty;
    }

#if NETSTANDARD2_0
    private static void StartService(IntPtr service)
#else
    private static void StartService(in IntPtr service)
#endif
    {
        _ = NativeMethods.StartService(service, 0, 0);
        var changedStatus = WaitForServiceStatus(service, ServiceState.StartPending, ServiceState.Running);
        if (!changedStatus)
        {
            throw new ApplicationException("Unable to start service");
        }
    }

#if NETSTANDARD2_0
    private static void StopService(IntPtr service)
#else
    private static void StopService(in IntPtr service)
#endif
    {
        var status = new NativeMethods.SERVICE_STATUS();
        _ = NativeMethods.ControlService(service, ServiceControl.Stop, status);
        var changedStatus = WaitForServiceStatus(service, ServiceState.StopPending, ServiceState.Stopped);
        if (!changedStatus)
        {
            throw new ApplicationException("Unable to stop service");
        }
    }

#if NETSTANDARD2_0
    private static ServiceState GetServiceStatus(IntPtr service)
#else
    private static ServiceState GetServiceStatus(in IntPtr service)
#endif
    {
        var status = new NativeMethods.SERVICE_STATUS();

        if (NativeMethods.QueryServiceStatus(service, status) == 0)
        {
            throw new ApplicationException("Failed to query service status.");
        }

        return status.dwCurrentState;
    }

#if NETSTANDARD2_0
    private static bool WaitForServiceStatus(IntPtr service, ServiceState waitStatus, ServiceState desiredStatus)
#else
    private static bool WaitForServiceStatus(in IntPtr service, ServiceState waitStatus, ServiceState desiredStatus)
#endif
    {
        var status = new NativeMethods.SERVICE_STATUS();

        _ = NativeMethods.QueryServiceStatus(service, status);
        if (status.dwCurrentState == desiredStatus)
        {
            return true;
        }

        var dwStartTickCount = Environment.TickCount;
        var dwOldCheckPoint = status.dwCheckPoint;

        while (status.dwCurrentState == waitStatus)
        {
            // Do not wait longer than the wait hint. A good interval is
            // one tenth the wait hint, but no less than 1 second and no
            // more than 10 seconds.
            var dwWaitTime = status.dwWaitHint / 10;

            if (dwWaitTime < 1000)
            {
                dwWaitTime = 1000;
            }
            else if (dwWaitTime > 10000)
            {
                dwWaitTime = 10000;
            }

            Thread.Sleep(dwWaitTime);

            // Check the status again.
            if (NativeMethods.QueryServiceStatus(service, status) == 0)
            {
                break;
            }

            if (status.dwCheckPoint > dwOldCheckPoint)
            {
                // The service is making progress.
                dwStartTickCount = Environment.TickCount;
                dwOldCheckPoint = status.dwCheckPoint;
            }
            else if (Environment.TickCount - dwStartTickCount > status.dwWaitHint)
            {
                // No progress made within the wait hint
                break;
            }
        }

        return status.dwCurrentState == desiredStatus;
    }

    private static IntPtr OpenSCManager(ScmAccessRights rights)
    {
        var scm = NativeMethods.OpenSCManager(null!, null!, rights);
        if (scm == IntPtr.Zero)
        {
            throw new ApplicationException("Could not connect to service control manager.");
        }

        return scm;
    }
}
