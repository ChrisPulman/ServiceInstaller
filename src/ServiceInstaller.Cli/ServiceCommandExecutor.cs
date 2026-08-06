// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using ServiceInstaller.Enums;

namespace ServiceInstaller.Cli;

/// <summary>Executes parsed service commands.</summary>
internal static class ServiceCommandExecutor
{
    /// <summary>The default command timeout in seconds.</summary>
    private const int DefaultTimeoutSeconds = 30;

    /// <summary>The dependency-list separators.</summary>
    private static readonly char[] DependencySeparators = [','];

    /// <summary>Executes a parsed request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="input">Standard input.</param>
    /// <param name="output">Standard output.</param>
    /// <returns>A successful exit code.</returns>
    internal static int Execute(
        CommandRequest request,
        IServiceManager manager,
        TextReader input,
        TextWriter output)
    {
        switch (request.Command)
        {
            case ServiceCommandVerb.Create or ServiceCommandVerb.Install:
                {
                    ExecuteCreate(request, manager, input, request.Command == ServiceCommandVerb.Install, output);
                    break;
                }

            case ServiceCommandVerb.Configure:
                {
                    ExecuteConfigure(request, manager, input, output);
                    break;
                }

            case ServiceCommandVerb.Status or ServiceCommandVerb.Query:
                {
                    ExecuteQuery(request, manager, output);
                    break;
                }

            case ServiceCommandVerb.Exists:
                {
                    ExecuteExists(request, manager, output);
                    break;
                }

            case ServiceCommandVerb.Start:
                {
                    ExecuteStart(request, manager, output);
                    break;
                }

            case ServiceCommandVerb.Stop or ServiceCommandVerb.Pause or ServiceCommandVerb.Continue or ServiceCommandVerb.Resume:
                {
                    ExecuteStateTransition(request, manager, output);
                    break;
                }

            case ServiceCommandVerb.Delete or ServiceCommandVerb.Uninstall:
                {
                    ExecuteDelete(request, manager, output);
                    break;
                }

            default:
                throw new ArgumentException($"Unknown command '{request.Verb}'.");
        }

        return (int)ServiceCommandExitCode.Success;
    }

    /// <summary>Executes a service creation request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="input">Standard input.</param>
    /// <param name="startAfterCreate">Whether to start after creation.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteCreate(
        CommandRequest request,
        IServiceManager manager,
        TextReader input,
        bool startAfterCreate,
        TextWriter output)
    {
        request.EnsureAllowed(
            CommandOptions.Machine,
            CommandOptions.Name,
            CommandOptions.DisplayName,
            CommandOptions.Binary,
            CommandOptions.Description,
            CommandOptions.StartMode,
            CommandOptions.Account,
            CommandOptions.PasswordStdin,
            CommandOptions.Dependencies,
            CommandOptions.Start,
            CommandOptions.Timeout);
        var serviceName = request.GetRequired(CommandOptions.Name);
        manager.Create(CreateDefinition(request, input, serviceName));
        output.WriteLine($"Service '{serviceName}' created successfully.");
        if (startAfterCreate || request.HasFlag(CommandOptions.Start))
        {
            WriteState(output, manager.Start(serviceName, GetTimeout(request)));
        }
    }

    /// <summary>Creates a service definition from a request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="input">Standard input.</param>
    /// <param name="serviceName">The stable service name.</param>
    /// <returns>The service definition.</returns>
    private static ServiceDefinition CreateDefinition(
        CommandRequest request,
        TextReader input,
        string serviceName) =>
        new(
            serviceName,
            request.GetRequired(CommandOptions.DisplayName),
            request.GetRequired(CommandOptions.Binary))
        {
            AccountName = request.Get(CommandOptions.Account),
            Arguments = request.ServiceArguments,
            Dependencies = SplitDependencies(request.Get(CommandOptions.Dependencies)),
            Description = request.Get(CommandOptions.Description),
            Password = ReadPassword(request, input),
            StartMode = ParseStartMode(request.Get(CommandOptions.StartMode)),
        };

    /// <summary>Executes a configuration request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="input">Standard input.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteConfigure(
        CommandRequest request,
        IServiceManager manager,
        TextReader input,
        TextWriter output)
    {
        request.EnsureAllowed(
            CommandOptions.Machine,
            CommandOptions.Name,
            CommandOptions.DisplayName,
            CommandOptions.Binary,
            CommandOptions.Description,
            CommandOptions.StartMode,
            CommandOptions.Account,
            CommandOptions.PasswordStdin,
            CommandOptions.Dependencies);
        manager.Configure(request.GetRequired(CommandOptions.Name), CreateUpdate(request, input));
        output.WriteLine($"Service '{request.GetRequired(CommandOptions.Name)}' configured successfully.");
    }

    /// <summary>Creates a service update from a request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="input">Standard input.</param>
    /// <returns>The service update.</returns>
    private static ServiceUpdate CreateUpdate(CommandRequest request, TextReader input)
    {
        if (request.ServiceArguments.Length > 0 && !request.Has(CommandOptions.Binary))
        {
            throw new ArgumentException("--binary is required when replacing service arguments.");
        }

        return new ServiceUpdate
        {
            AccountName = request.Get(CommandOptions.Account),
            Arguments = request.ServiceArguments.Length == 0 ? null : request.ServiceArguments,
            ChangeAccount = request.Has(CommandOptions.Account) || request.HasFlag(CommandOptions.PasswordStdin),
            ChangeDescription = request.Has(CommandOptions.Description),
            Dependencies = request.Has(CommandOptions.Dependencies)
                ? SplitDependencies(request.Get(CommandOptions.Dependencies))
                : null,
            Description = request.Get(CommandOptions.Description),
            DisplayName = request.Get(CommandOptions.DisplayName),
            ExecutablePath = request.Get(CommandOptions.Binary),
            Password = ReadPassword(request, input),
            StartMode = request.Has(CommandOptions.StartMode)
                ? ParseStartMode(request.Get(CommandOptions.StartMode))
                : null,
        };
    }

    /// <summary>Executes an existence request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteExists(CommandRequest request, IServiceManager manager, TextWriter output)
    {
        request.EnsureAllowed(CommandOptions.Machine, CommandOptions.Name);
        output.WriteLine(manager.Exists(request.GetRequired(CommandOptions.Name)) ? "true" : "false");
    }

    /// <summary>Executes a start request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteStart(CommandRequest request, IServiceManager manager, TextWriter output)
    {
        request.EnsureAllowed(CommandOptions.Machine, CommandOptions.Name, CommandOptions.Timeout);
        var status = manager.Start(
            request.GetRequired(CommandOptions.Name),
            GetTimeout(request),
            request.ServiceArguments);
        WriteState(output, status);
    }

    /// <summary>Executes a service state transition.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteStateTransition(
        CommandRequest request,
        IServiceManager manager,
        TextWriter output)
    {
        request.EnsureAllowed(CommandOptions.Machine, CommandOptions.Name, CommandOptions.Timeout);
        var name = request.GetRequired(CommandOptions.Name);
        var timeout = GetTimeout(request);
        var status = request.Command switch
        {
            ServiceCommandVerb.Stop => manager.Stop(name, timeout),
            ServiceCommandVerb.Pause => manager.Pause(name, timeout),
            _ => manager.Continue(name, timeout),
        };
        WriteState(output, status);
    }

    /// <summary>Executes a deletion request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteDelete(CommandRequest request, IServiceManager manager, TextWriter output)
    {
        request.EnsureAllowed(CommandOptions.Machine, CommandOptions.Name, CommandOptions.Timeout);
        output.WriteLine(
            manager.Delete(request.GetRequired(CommandOptions.Name), GetTimeout(request))
                ? "Service marked for deletion."
                : "Service is not installed.");
    }

    /// <summary>Executes a query request.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="manager">The service manager.</param>
    /// <param name="output">Standard output.</param>
    private static void ExecuteQuery(CommandRequest request, IServiceManager manager, TextWriter output)
    {
        request.EnsureAllowed(CommandOptions.Machine, CommandOptions.Name, CommandOptions.Json);
        var snapshot = manager.Query(request.GetRequired(CommandOptions.Name));
        if (request.HasFlag(CommandOptions.Json))
        {
            output.WriteLine(JsonSerializer.Serialize(snapshot));
            return;
        }

        WriteSnapshot(output, snapshot);
    }

    /// <summary>Writes a human-readable service snapshot.</summary>
    /// <param name="output">Standard output.</param>
    /// <param name="snapshot">The service snapshot.</param>
    private static void WriteSnapshot(TextWriter output, ServiceSnapshot snapshot)
    {
        output.WriteLine($"Name: {snapshot.Configuration.ServiceName}");
        output.WriteLine($"Display name: {snapshot.Configuration.DisplayName}");
        output.WriteLine($"State: {snapshot.Status.State}");
        output.WriteLine($"Start mode: {snapshot.Configuration.StartMode}");
        output.WriteLine($"Image path: {snapshot.Configuration.ImagePath}");
        output.WriteLine($"Account: {snapshot.Configuration.AccountName}");
        output.WriteLine($"Description: {snapshot.Configuration.Description ?? string.Empty}");
        output.WriteLine($"Process ID: {snapshot.Status.ProcessId}");
    }

    /// <summary>Reads a password from standard input when requested.</summary>
    /// <param name="request">The parsed request.</param>
    /// <param name="input">Standard input.</param>
    /// <returns>The supplied password, or <see langword="null"/>.</returns>
    private static string? ReadPassword(CommandRequest request, TextReader input) =>
        !request.HasFlag(CommandOptions.PasswordStdin)
            ? null
            : input.ReadLine()
                ?? throw new ArgumentException("No password was available on standard input.");

    /// <summary>Splits a comma-separated dependency list.</summary>
    /// <param name="value">The dependency option value.</param>
    /// <returns>The dependency names.</returns>
    private static string[] SplitDependencies(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var parts = value.Split(DependencySeparators, StringSplitOptions.RemoveEmptyEntries);
        var dependencies = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            var dependency = part.Trim();
            if (dependency.Length > 0)
            {
                dependencies.Add(dependency);
            }
        }

        return dependencies.ToArray();
    }

    /// <summary>Parses a service start mode.</summary>
    /// <param name="value">The start mode text.</param>
    /// <returns>The service start mode.</returns>
    private static ServiceStartMode ParseStartMode(string? value) => value?.ToLowerInvariant() switch
    {
        null or "automatic" or "auto" => ServiceStartMode.Automatic,
        "manual" or "demand" => ServiceStartMode.Manual,
        "disabled" => ServiceStartMode.Disabled,
        _ => throw new ArgumentException($"Unknown start mode '{value}'. Use automatic, manual, or disabled."),
    };

    /// <summary>Parses the transition timeout.</summary>
    /// <param name="request">The parsed request.</param>
    /// <returns>The transition timeout.</returns>
    private static TimeSpan GetTimeout(CommandRequest request)
    {
        var value = request.Get(CommandOptions.Timeout);
        if (value is null)
        {
            return TimeSpan.FromSeconds(DefaultTimeoutSeconds);
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || seconds <= 0)
        {
            throw new FormatException("The --timeout value must be a positive number of seconds.");
        }

        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>Writes a service state.</summary>
    /// <param name="output">Standard output.</param>
    /// <param name="status">The service status.</param>
    private static void WriteState(TextWriter output, ServiceStatus status) =>
        output.WriteLine($"Service state: {status.State}");
}
