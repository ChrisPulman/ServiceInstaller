// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Parses terminal arguments and executes Windows service operations.</summary>
public sealed class ServiceCommandLine
{
    /// <summary>The Win32 access-denied error code.</summary>
    private const int AccessDeniedError = 5;

    /// <summary>The service-manager factory.</summary>
    private readonly Func<string?, IServiceManager> _managerFactory;

    /// <summary>Initializes a new instance of the <see cref="ServiceCommandLine"/> class.</summary>
    public ServiceCommandLine()
        : this(CreateManager)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ServiceCommandLine"/> class.</summary>
    /// <param name="managerFactory">Creates the manager used for a command.</param>
    internal ServiceCommandLine(Func<string?, IServiceManager> managerFactory)
    {
        _managerFactory = managerFactory ?? throw new ArgumentNullException(nameof(managerFactory));
    }

    /// <summary>Executes a CLI request.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="input">Input used for secret values such as an account password.</param>
    /// <param name="output">Standard command output.</param>
    /// <param name="error">Error output.</param>
    /// <returns>A stable <see cref="ServiceCommandExitCode"/> value.</returns>
    public int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                output.Write(Usage.Text);
                return (int)ServiceCommandExitCode.Success;
            }

            var request = CommandRequest.Parse(args);
            var manager = _managerFactory(request.Get(CommandOptions.Machine));
            return ServiceCommandExecutor.Execute(request, manager, input, output);
        }
        catch (ServiceOperationException exception) when (exception.NativeErrorCode == AccessDeniedError)
        {
            error.WriteLine(exception.Message);
            return (int)ServiceCommandExitCode.PlatformOrAccessDenied;
        }
        catch (PlatformNotSupportedException exception)
        {
            error.WriteLine(exception.Message);
            return (int)ServiceCommandExitCode.PlatformOrAccessDenied;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            error.WriteLine(exception.Message);
            error.Write(Usage.Text);
            return (int)ServiceCommandExitCode.InvalidArguments;
        }
        catch (Exception exception) when (
            exception is ServiceOperationException or InvalidOperationException or TimeoutException)
        {
            error.WriteLine(exception.Message);
            return (int)ServiceCommandExitCode.OperationFailed;
        }
    }

    /// <summary>Creates a Windows service manager.</summary>
    /// <param name="machineName">Optional remote computer name.</param>
    /// <returns>The service manager.</returns>
    private static IServiceManager CreateManager(string? machineName) => new WindowsServiceManager(machineName);

    /// <summary>Determines whether a command requests help.</summary>
    /// <param name="value">The first command token.</param>
    /// <returns><see langword="true"/> when help was requested.</returns>
    private static bool IsHelp(string value) => value is "help" or "--help" or "-h" or "/?";
}
