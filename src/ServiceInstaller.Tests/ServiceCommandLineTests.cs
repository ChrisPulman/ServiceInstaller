// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Cli;
using ServiceInstaller.Enums;

namespace ServiceInstaller.Tests;

/// <summary>Tests terminal command parsing, execution, and exit-code mapping.</summary>
public sealed class ServiceCommandLineTests
{
    /// <summary>The Win32 access-denied error.</summary>
    private const int AccessDeniedError = 5;

    /// <summary>The sample service account.</summary>
    private const string AccountName = "account";

    /// <summary>The executable path option.</summary>
    private const string BinaryOption = "--binary";

    /// <summary>The create command.</summary>
    private const string CreateCommand = "create";

    /// <summary>The configure command.</summary>
    private const string ConfigureCommand = "configure";

    /// <summary>The default timeout in seconds.</summary>
    private const int DefaultTimeoutSeconds = 30;

    /// <summary>The sample dependency name.</summary>
    private const string DependencyName = "RpcSs";

    /// <summary>The sample display name.</summary>
    private const string DisplayName = "Sample";

    /// <summary>The display-name option.</summary>
    private const string DisplayNameOption = "--display-name";

    /// <summary>The sample executable path.</summary>
    private const string ExecutablePath = "sample.exe";

    /// <summary>The exists command.</summary>
    private const string ExistsCommand = "exists";

    /// <summary>The JSON output option.</summary>
    private const string JsonOption = "--json";

    /// <summary>The stable-name option key.</summary>
    private const string NameKey = "name";

    /// <summary>The stable-name option.</summary>
    private const string NameOption = "--name";

    /// <summary>The standard-input password option.</summary>
    private const string PasswordStdinOption = "--password-stdin";

    /// <summary>The sample stable service name.</summary>
    private const string ServiceName = "sample";

    /// <summary>The start command.</summary>
    private const string StartCommand = "start";

    /// <summary>The start-mode option.</summary>
    private const string StartModeOption = "--start-mode";

    /// <summary>The create test timeout in seconds.</summary>
    private const int StartTimeoutSeconds = 12;

    /// <summary>The state-command timeout in seconds.</summary>
    private const int StateTimeoutSeconds = 4;

    /// <summary>The timeout option.</summary>
    private const string TimeoutOption = "--timeout";

    /// <summary>Verifies help aliases and the process entry point.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Help_and_entry_point_return_success()
    {
        foreach (var help in new[] { "help", "--help", "-h", "/?" })
        {
            var result = Run(new RecordingServiceManager(), [help]);
            await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
            await Assert.That(result.Output).Contains("ServiceInstaller CLI");
        }

        var empty = Run(new RecordingServiceManager(), []);
        var main = Program.Main(["help"]);
        var defaultFactory = Run(new ServiceCommandLine(), ["unknown"]);

        await Assert.That(empty.Output).Contains("Usage:");
        await Assert.That(main).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(defaultFactory.ExitCode).IsEqualTo((int)ServiceCommandExitCode.InvalidArguments);
    }

    /// <summary>Verifies required stream dependencies are validated.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Run_validates_all_stream_dependencies()
    {
        var commandLine = new ServiceCommandLine(static _ => new RecordingServiceManager());

        await Assert.That(() => commandLine.Run(null!, TextReader.Null, TextWriter.Null, TextWriter.Null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => commandLine.Run([], null!, TextWriter.Null, TextWriter.Null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => commandLine.Run([], TextReader.Null, null!, TextWriter.Null))
            .Throws<ArgumentNullException>();
        await Assert.That(() => commandLine.Run([], TextReader.Null, TextWriter.Null, null!))
            .Throws<ArgumentNullException>();
        await Assert.That(static () => new ServiceCommandLine(null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies create maps all service options.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Create_maps_all_options_and_can_start_immediately()
    {
        var manager = new RecordingServiceManager();
        string? machineName = null;
        var commandLine = new ServiceCommandLine(machine =>
        {
            machineName = machine;
            return manager;
        });
        var result = Run(
            commandLine,
            [
                CreateCommand, "--machine", "remote", NameOption, ServiceName, DisplayNameOption, DisplayName,
                BinaryOption, ExecutablePath, "--description", "Description", StartModeOption, "manual",
                "--account", AccountName, PasswordStdinOption, "--dependencies", $"{DependencyName}, EventLog",
                "--start", TimeoutOption, StartTimeoutSeconds.ToString(), "--", "--service", "two words",
            ],
            "password\n");

        await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(machineName).IsEqualTo("remote");
        await Assert.That(manager.Definition).IsNotNull();
        await Assert.That(manager.Definition!.ServiceName).IsEqualTo(ServiceName);
        await Assert.That(manager.Definition.DisplayName).IsEqualTo(DisplayName);
        await Assert.That(manager.Definition.ExecutablePath).IsEqualTo(ExecutablePath);
        await Assert.That(manager.Definition.Description).IsEqualTo("Description");
        await Assert.That(manager.Definition.StartMode).IsEqualTo(ServiceStartMode.Manual);
        await Assert.That(manager.Definition.AccountName).IsEqualTo(AccountName);
        await Assert.That(manager.Definition.Password).IsEqualTo("password");
        await Assert.That(manager.Definition.Dependencies).IsEquivalentTo([DependencyName, "EventLog"]);
        await Assert.That(manager.Definition.Arguments).IsEquivalentTo(["--service", "two words"]);
        await Assert.That(manager.Operations).Contains("start:sample");
        await Assert.That(manager.Timeout).IsEqualTo(TimeSpan.FromSeconds(StartTimeoutSeconds));
        await Assert.That(result.Output).Contains("created successfully");
        await Assert.That(result.Output).Contains("Service state: Running");
    }

    /// <summary>Verifies install starts with the default timeout.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Install_alias_starts_with_the_default_timeout()
    {
        var manager = new RecordingServiceManager();

        var result = Run(
            manager,
            ["install", NameOption, ServiceName, DisplayNameOption, DisplayName, BinaryOption, ExecutablePath]);

        await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(manager.Operations).Contains("start:sample");
        await Assert.That(manager.Timeout).IsEqualTo(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
    }

    /// <summary>Verifies all start-mode aliases.</summary>
    /// <param name="value">The start-mode alias.</param>
    /// <param name="expected">The expected start mode.</param>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    [Arguments(null, ServiceStartMode.Automatic)]
    [Arguments("automatic", ServiceStartMode.Automatic)]
    [Arguments("auto", ServiceStartMode.Automatic)]
    [Arguments("manual", ServiceStartMode.Manual)]
    [Arguments("demand", ServiceStartMode.Manual)]
    [Arguments("disabled", ServiceStartMode.Disabled)]
    public async Task Create_accepts_all_start_mode_aliases(string? value, ServiceStartMode expected)
    {
        var manager = new RecordingServiceManager();
        var args = new List<string>
        {
            CreateCommand, NameOption, ServiceName, DisplayNameOption, DisplayName, BinaryOption, ExecutablePath,
        };
        if (value is not null)
        {
            args.Add(StartModeOption);
            args.Add(value);
        }

        var result = Run(manager, args.ToArray());

        await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(manager.Definition!.StartMode).IsEqualTo(expected);
    }

    /// <summary>Verifies configure maps changed and unchanged values.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Configure_maps_changed_and_unchanged_values()
    {
        var manager = new RecordingServiceManager();

        var changed = Run(
            manager,
            [
                ConfigureCommand, NameOption, ServiceName, DisplayNameOption, "New Sample", BinaryOption, "new.exe",
                "--description", string.Empty, StartModeOption, "disabled", "--account", AccountName,
                PasswordStdinOption, "--dependencies", DependencyName, "--", "--new",
            ],
            "secret\n");

        await Assert.That(changed.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(manager.Update).IsNotNull();
        await Assert.That(manager.Update!.DisplayName).IsEqualTo("New Sample");
        await Assert.That(manager.Update.ExecutablePath).IsEqualTo("new.exe");
        await Assert.That(manager.Update.Arguments).IsEquivalentTo(["--new"]);
        await Assert.That(manager.Update.StartMode).IsEqualTo(ServiceStartMode.Disabled);
        await Assert.That(manager.Update.ChangeDescription).IsTrue();
        await Assert.That(manager.Update.Description).IsEmpty();
        await Assert.That(manager.Update.ChangeAccount).IsTrue();
        await Assert.That(manager.Update.AccountName).IsEqualTo(AccountName);
        await Assert.That(manager.Update.Password).IsEqualTo("secret");
        await Assert.That(manager.Update.Dependencies).IsEquivalentTo([DependencyName]);

        var unchangedManager = new RecordingServiceManager();
        var unchanged = Run(unchangedManager, [ConfigureCommand, NameOption, ServiceName]);
        await Assert.That(unchanged.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(unchangedManager.Update!.Arguments).IsNull();
        await Assert.That(unchangedManager.Update.StartMode).IsNull();
        await Assert.That(unchangedManager.Update.Dependencies).IsNull();
        await Assert.That(unchangedManager.Update.ChangeDescription).IsFalse();
        await Assert.That(unchangedManager.Update.ChangeAccount).IsFalse();
    }

    /// <summary>Verifies query supports text and JSON output.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Query_supports_text_and_json_output()
    {
        var manager = new RecordingServiceManager();

        var text = Run(manager, ["status", NameOption, ServiceName]);
        var json = Run(manager, ["query", NameOption, ServiceName, JsonOption]);

        await Assert.That(text.Output).Contains("Name: sample");
        await Assert.That(text.Output).Contains("Display name: Sample");
        await Assert.That(text.Output).Contains("State: Running");
        await Assert.That(text.Output).Contains("Start mode: Automatic");
        await Assert.That(text.Output).Contains("Image path:");
        await Assert.That(text.Output).Contains("Account: LocalSystem");
        await Assert.That(text.Output).Contains("Description:");
        await Assert.That(text.Output).Contains("Process ID: 0");
        await Assert.That(json.Output).Contains("\"Configuration\"");
        await Assert.That(json.Output).Contains("\"Status\"");
    }

    /// <summary>Verifies existence and deletion outcomes.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Exists_and_delete_report_boolean_outcomes()
    {
        var manager = new RecordingServiceManager { ExistsResult = true };

        var exists = Run(manager, [ExistsCommand, NameOption, ServiceName]);
        manager.ExistsResult = false;
        var absent = Run(manager, [ExistsCommand, NameOption, ServiceName]);
        var deleted = Run(manager, ["delete", NameOption, ServiceName]);
        manager.DeleteResult = false;
        var missing = Run(manager, ["uninstall", NameOption, ServiceName]);

        await Assert.That(exists.Output.Trim()).IsEqualTo("true");
        await Assert.That(absent.Output.Trim()).IsEqualTo("false");
        await Assert.That(deleted.Output).Contains("marked for deletion");
        await Assert.That(missing.Output).Contains("not installed");
    }

    /// <summary>Verifies state commands delegate to the manager.</summary>
    /// <param name="command">The state command.</param>
    /// <param name="expectedOperation">The expected recorded operation.</param>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    [Arguments("stop", "stop:sample")]
    [Arguments("pause", "pause:sample")]
    [Arguments("continue", "continue:sample")]
    [Arguments("resume", "continue:sample")]
    public async Task State_commands_delegate_to_the_manager(string command, string expectedOperation)
    {
        var manager = new RecordingServiceManager();

        var result = Run(manager, [command, NameOption, ServiceName, TimeoutOption, StateTimeoutSeconds.ToString()]);

        await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(manager.Operations).Contains(expectedOperation);
        await Assert.That(manager.Timeout).IsEqualTo(TimeSpan.FromSeconds(StateTimeoutSeconds));
        await Assert.That(result.Output).Contains("Service state: Running");
    }

    /// <summary>Verifies start forwards runtime arguments.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Start_forwards_runtime_arguments()
    {
        var manager = new RecordingServiceManager();

        var result = Run(manager, [StartCommand, NameOption, ServiceName, "--", "one", "two"]);

        await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.Success);
        await Assert.That(manager.StartArguments).IsEquivalentTo(["one", "two"]);
    }

    /// <summary>Verifies invalid requests map to argument errors.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Invalid_terminal_requests_return_argument_errors()
    {
        var manager = new RecordingServiceManager();
        var cases = new[]
        {
            new[] { "unknown" },
            new[] { StartCommand, NameOption },
            new[] { StartCommand, NameKey },
            new[] { ExistsCommand, NameOption, ServiceName, JsonOption },
            new[] { CreateCommand, NameOption, ServiceName, DisplayNameOption, DisplayName, BinaryOption, ExecutablePath, StartModeOption, "invalid" },
            new[] { StartCommand, NameOption, ServiceName, TimeoutOption, "0" },
            new[] { StartCommand, NameOption, ServiceName, TimeoutOption, "text" },
            new[] { ConfigureCommand, NameOption, ServiceName, "--", "replacement" },
        };

        foreach (var args in cases)
        {
            var result = Run(manager, args);
            await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.InvalidArguments);
            await Assert.That(result.Error).Contains("ServiceInstaller CLI");
        }
    }

    /// <summary>Verifies requested password input is required.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Password_input_is_required_when_requested()
    {
        var result = Run(
            new RecordingServiceManager(),
            [
                CreateCommand, NameOption, ServiceName, DisplayNameOption, DisplayName, BinaryOption, ExecutablePath,
                PasswordStdinOption,
            ]);

        await Assert.That(result.ExitCode).IsEqualTo((int)ServiceCommandExitCode.InvalidArguments);
        await Assert.That(result.Error).Contains("No password");
    }

    /// <summary>Verifies operational failures map to stable exit codes.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Operational_failures_map_to_stable_exit_codes()
    {
        var accessDenied = new RecordingServiceManager
        {
            Failure = new ServiceOperationException("OpenService", ServiceName, AccessDeniedError, "denied"),
        };
        var operationFailed = new RecordingServiceManager { Failure = new ServiceOperationException("failure") };
        var invalidState = new RecordingServiceManager { Failure = new InvalidOperationException("state") };
        var timeout = new RecordingServiceManager { Failure = new TimeoutException("timeout") };

        var deniedResult = Run(accessDenied, [ExistsCommand, NameOption, ServiceName]);
        var operationResult = Run(operationFailed, [ExistsCommand, NameOption, ServiceName]);
        var stateResult = Run(invalidState, [ExistsCommand, NameOption, ServiceName]);
        var timeoutResult = Run(timeout, [ExistsCommand, NameOption, ServiceName]);
        var platformResult = Run(
            new ServiceCommandLine(static _ => throw new PlatformNotSupportedException("platform")),
            [ExistsCommand, NameOption, ServiceName]);

        await Assert.That(deniedResult.ExitCode).IsEqualTo((int)ServiceCommandExitCode.PlatformOrAccessDenied);
        await Assert.That(platformResult.ExitCode).IsEqualTo((int)ServiceCommandExitCode.PlatformOrAccessDenied);
        await Assert.That(operationResult.ExitCode).IsEqualTo((int)ServiceCommandExitCode.OperationFailed);
        await Assert.That(stateResult.ExitCode).IsEqualTo((int)ServiceCommandExitCode.OperationFailed);
        await Assert.That(timeoutResult.ExitCode).IsEqualTo((int)ServiceCommandExitCode.OperationFailed);
    }

    /// <summary>Verifies direct request parsing and duplicate detection.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Command_request_detects_duplicates_and_invalid_options()
    {
        var request = CommandRequest.Parse([StartCommand, NameOption, ServiceName, JsonOption, "--", "one"]);

        await Assert.That(request.Verb).IsEqualTo(StartCommand);
        await Assert.That(request.Get(NameKey)).IsEqualTo(ServiceName);
        await Assert.That(request.Get("missing")).IsNull();
        await Assert.That(request.Has(NameKey)).IsTrue();
        await Assert.That(request.HasFlag("json")).IsTrue();
        await Assert.That(request.ServiceArguments).IsEquivalentTo(["one"]);
        await Assert.That(request.GetRequired(NameKey)).IsEqualTo(ServiceName);
        await Assert.That(() => request.GetRequired("missing")).Throws<ArgumentException>();
        await Assert.That(() => request.EnsureAllowed(NameKey)).Throws<ArgumentException>();
        await Assert.That(static () => CommandRequest.Parse([StartCommand, NameOption, "one", NameOption, "two"]))
            .Throws<ArgumentException>();
        await Assert.That(static () => CommandRequest.Parse([StartCommand, JsonOption, JsonOption]))
            .Throws<ArgumentException>();
        await Assert.That(static () => CommandRequest.Parse([StartCommand, JsonOption, NameOption, "one", JsonOption, "true"]))
            .Throws<ArgumentException>();
        await Assert.That(static () => CommandRequest.Parse([StartCommand, JsonOption, "true", JsonOption]))
            .Throws<ArgumentException>();
    }

    /// <summary>Runs a command with a recording manager.</summary>
    /// <param name="manager">The recording manager.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="input">Standard input.</param>
    /// <returns>The exit code and captured output.</returns>
    private static (int ExitCode, string Output, string Error) Run(
        RecordingServiceManager manager,
        string[] args,
        string input = "") =>
        Run(new ServiceCommandLine(_ => manager), args, input);

    /// <summary>Runs a prepared command line.</summary>
    /// <param name="commandLine">The command-line runner.</param>
    /// <param name="args">The command arguments.</param>
    /// <param name="input">Standard input.</param>
    /// <returns>The exit code and captured output.</returns>
    private static (int ExitCode, string Output, string Error) Run(
        ServiceCommandLine commandLine,
        string[] args,
        string input = "")
    {
        using var reader = new StringReader(input);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exitCode = commandLine.Run(args, reader, output, error);
        return (exitCode, output.ToString(), error.ToString());
    }
}
