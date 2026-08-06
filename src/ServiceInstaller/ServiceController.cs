// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Text;
using ServiceInstaller.Internal;

namespace ServiceInstaller;

/// <summary>Provides the legacy single-argument command adapter.</summary>
public static class ServiceController
{
    /// <summary>Known legacy service command names.</summary>
    private static readonly Dictionary<string, LegacyServiceCommand> KnownCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["-continue"] = LegacyServiceCommand.Continue,
        ["-install"] = LegacyServiceCommand.Install,
        ["-isinstalled"] = LegacyServiceCommand.IsInstalled,
        ["-pause"] = LegacyServiceCommand.Pause,
        ["-resume"] = LegacyServiceCommand.Resume,
        ["-start"] = LegacyServiceCommand.Start,
        ["-status"] = LegacyServiceCommand.Status,
        ["-stop"] = LegacyServiceCommand.Stop,
        ["-uninstall"] = LegacyServiceCommand.Uninstall,
    };

    /// <summary>The application-specific help entries.</summary>
    private static string[] _commands = [];

    /// <summary>Adds application-specific commands to the legacy help output.</summary>
    /// <param name="commands">The application-specific command descriptions.</param>
    public static void AddApplicationArguments(params string[] commands) =>
        Volatile.Write(ref _commands, commands?.ToArray() ?? Array.Empty<string>());

    /// <summary>Handles a legacy service command.</summary>
    /// <param name="command">A command such as <c>-Install</c>, <c>-Start</c>, or <c>-Status</c>.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="displayName">The display name used during installation.</param>
    /// <param name="parameters">Arguments stored in the installed service image path.</param>
    /// <returns>A human-readable operation result.</returns>
    public static string HandleRequest(
        string? command,
        string serviceName,
        string displayName,
        params string[] parameters) =>
        HandleRequest(
            command,
            serviceName,
            displayName,
            new WindowsServiceManager(),
            new ApplicationPathResolver(),
            parameters ?? Array.Empty<string>());

    /// <summary>Handles a legacy request through composed service dependencies.</summary>
    /// <param name="command">The legacy command.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="displayName">The service display name.</param>
    /// <param name="serviceManager">The service manager.</param>
    /// <param name="pathResolver">The application path resolver.</param>
    /// <param name="parameters">The stored service arguments.</param>
    /// <returns>The human-readable result.</returns>
    internal static string HandleRequest(
        string? command,
        string serviceName,
        string displayName,
        IServiceManager serviceManager,
        IApplicationPathResolver pathResolver,
        IReadOnlyList<string> parameters)
    {
        serviceManager = Guard.NotNull(serviceManager, nameof(serviceManager));
        pathResolver = Guard.NotNull(pathResolver, nameof(pathResolver));

        try
        {
            return Execute(
                command,
                serviceName,
                displayName,
                serviceManager,
                pathResolver,
                parameters);
        }
        catch (Exception exception)
        {
            return $"Service request failed: {exception.Message}";
        }
    }

    /// <summary>Executes a validated legacy request.</summary>
    /// <param name="command">The legacy command.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="displayName">The service display name.</param>
    /// <param name="serviceManager">The service manager.</param>
    /// <param name="pathResolver">The application path resolver.</param>
    /// <param name="parameters">The stored service arguments.</param>
    /// <returns>The human-readable result.</returns>
    private static string Execute(
        string? command,
        string serviceName,
        string displayName,
        IServiceManager serviceManager,
        IApplicationPathResolver pathResolver,
        IReadOnlyList<string> parameters)
    {
        var timeout = WindowsServiceManager.GetDefaultTimeout();
        switch (ParseCommand(command))
        {
            case LegacyServiceCommand.Install:
                {
                    Install(serviceName, displayName, serviceManager, pathResolver, parameters, timeout);
                    return "Service installed and started successfully";
                }

            case LegacyServiceCommand.Uninstall:
                return serviceManager.Delete(serviceName, timeout)
                    ? "Service uninstalled successfully"
                    : "Service is not installed";

            case LegacyServiceCommand.Start:
                {
                    _ = serviceManager.Start(serviceName, timeout);
                    return "Service started successfully";
                }

            case LegacyServiceCommand.Stop:
                {
                    _ = serviceManager.Stop(serviceName, timeout);
                    return "Service stopped successfully";
                }

            case LegacyServiceCommand.Pause:
                {
                    _ = serviceManager.Pause(serviceName, timeout);
                    return "Service paused successfully";
                }

            case LegacyServiceCommand.Continue or LegacyServiceCommand.Resume:
                {
                    _ = serviceManager.Continue(serviceName, timeout);
                    return "Service continued successfully";
                }

            case LegacyServiceCommand.Status:
                return serviceManager.Query(serviceName).Status.State.ToString();

            case LegacyServiceCommand.IsInstalled:
                return $"The service {(serviceManager.Exists(serviceName) ? "is" : "is not")} installed";

            default:
                return GetUsage();
        }
    }

    /// <summary>Parses a legacy service command.</summary>
    /// <param name="command">The command text.</param>
    /// <returns>The parsed command.</returns>
    private static LegacyServiceCommand ParseCommand(string? command)
    {
        if (command is null)
        {
            return LegacyServiceCommand.Unknown;
        }

        return KnownCommands.TryGetValue(command.Trim(), out var parsed)
            ? parsed
            : LegacyServiceCommand.Unknown;
    }

    /// <summary>Creates a missing service and starts it.</summary>
    /// <param name="serviceName">The stable service name.</param>
    /// <param name="displayName">The service display name.</param>
    /// <param name="serviceManager">The service manager.</param>
    /// <param name="pathResolver">The application path resolver.</param>
    /// <param name="parameters">The stored service arguments.</param>
    /// <param name="timeout">The start timeout.</param>
    private static void Install(
        string serviceName,
        string displayName,
        IServiceManager serviceManager,
        IApplicationPathResolver pathResolver,
        IReadOnlyList<string> parameters,
        TimeSpan timeout)
    {
        if (!serviceManager.Exists(serviceName))
        {
            var definition = new ServiceDefinition(serviceName, displayName, pathResolver.Resolve())
            {
                Arguments = parameters,
            };
            serviceManager.Create(definition);
        }

        _ = serviceManager.Start(serviceName, timeout);
    }

    /// <summary>Builds the legacy help output.</summary>
    /// <returns>The help text.</returns>
    private static string GetUsage()
    {
        var builder = new StringBuilder()
            .AppendLine("Valid Service Arguments are:")
            .AppendLine("-Install")
            .AppendLine("-Uninstall")
            .AppendLine("-Start")
            .AppendLine("-Stop")
            .AppendLine("-Pause")
            .AppendLine("-Continue")
            .AppendLine("-Status")
            .AppendLine("-IsInstalled");
        var commands = Volatile.Read(ref _commands);
        if (commands.Length == 0)
        {
            return builder.ToString();
        }

        _ = builder.AppendLine("Additional Arguments:");
        foreach (var item in commands)
        {
            _ = builder.AppendLine(item);
        }

        return builder.ToString();
    }
}
