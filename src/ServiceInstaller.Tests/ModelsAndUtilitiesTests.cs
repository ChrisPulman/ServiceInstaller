// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;
using ServiceInstaller.Internal;

namespace ServiceInstaller.Tests;

/// <summary>Tests public models and deterministic utility components.</summary>
public sealed class ModelsAndUtilitiesTests
{
    /// <summary>The expected pending checkpoint.</summary>
    private const uint Checkpoint = 7;

    /// <summary>The expected service dependency.</summary>
    private const string DependencyName = "RpcSs";

    /// <summary>The expected service description.</summary>
    private const string Description = "description";

    /// <summary>The expected service display name.</summary>
    private const string DisplayName = "Sample";

    /// <summary>The expected executable path.</summary>
    private const string ExecutablePath = "service.exe";

    /// <summary>A reusable guard value.</summary>
    private const string GuardValue = "value";

    /// <summary>The expected installed image path.</summary>
    private const string ImagePath = "\"service.exe\"";

    /// <summary>The expected native error code.</summary>
    private const int NativeErrorCode = 5;

    /// <summary>The expected process identifier.</summary>
    private const uint ProcessIdentifier = 42;

    /// <summary>The expected stable service name.</summary>
    private const string ServiceName = "sample";

    /// <summary>The expected native wait hint.</summary>
    private const uint WaitHint = 900;

    /// <summary>Verifies service definitions preserve all values.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Models_preserve_all_supplied_values()
    {
        var arguments = new[] { "--run" };
        var dependencies = new[] { DependencyName };
        var definition = new ServiceDefinition(ServiceName, DisplayName, ExecutablePath)
        {
            AccountName = "account",
            Arguments = arguments,
            Dependencies = dependencies,
            Description = ModelsAndUtilitiesTests.Description,
            Password = "password",
            StartMode = ServiceStartMode.Manual,
        };

        await Assert.That(definition.ServiceName).IsEqualTo(ServiceName);
        await Assert.That(definition.DisplayName).IsEqualTo(DisplayName);
        await Assert.That(definition.ExecutablePath).IsEqualTo(ExecutablePath);
        await Assert.That(definition.Arguments).IsSameReferenceAs(arguments);
        await Assert.That(definition.Dependencies).IsSameReferenceAs(dependencies);
        await Assert.That(definition.Description).IsEqualTo(Description);
        await Assert.That(definition.AccountName).IsEqualTo("account");
        await Assert.That(definition.Password).IsEqualTo("password");
        await Assert.That(definition.StartMode).IsEqualTo(ServiceStartMode.Manual);
    }

    /// <summary>Verifies service updates and snapshots preserve all values.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Updates_and_snapshots_preserve_all_supplied_values()
    {
        var arguments = new[] { "--run" };
        var dependencies = new[] { DependencyName };
        var update = new ServiceUpdate
        {
            AccountName = "new-account",
            Arguments = arguments,
            ChangeAccount = true,
            ChangeDescription = true,
            Dependencies = dependencies,
            Description = "new description",
            DisplayName = "New Sample",
            ExecutablePath = "new-service.exe",
            Password = "new-password",
            StartMode = ServiceStartMode.Disabled,
        };
        var configuration = new ServiceConfiguration(
            ServiceName,
            DisplayName,
            ImagePath,
            ServiceStartMode.Automatic,
            "LocalSystem",
            ModelsAndUtilitiesTests.Description);
        var status = new ServiceStatus(
            ServiceState.Running,
            ServiceAcceptedControls.Stop | ServiceAcceptedControls.PauseContinue,
            ProcessIdentifier,
            Checkpoint,
            WaitHint);
        var snapshot = new ServiceSnapshot(configuration, status);

        await Assert.That(update.DisplayName).IsEqualTo("New Sample");
        await Assert.That(update.ExecutablePath).IsEqualTo("new-service.exe");
        await Assert.That(update.Arguments).IsSameReferenceAs(arguments);
        await Assert.That(update.StartMode).IsEqualTo(ServiceStartMode.Disabled);
        await Assert.That(update.Description).IsEqualTo("new description");
        await Assert.That(update.ChangeDescription).IsTrue();
        await Assert.That(update.AccountName).IsEqualTo("new-account");
        await Assert.That(update.Password).IsEqualTo("new-password");
        await Assert.That(update.ChangeAccount).IsTrue();
        await Assert.That(update.Dependencies).IsSameReferenceAs(dependencies);
        await Assert.That(configuration.ServiceName).IsEqualTo(ServiceName);
        await Assert.That(configuration.DisplayName).IsEqualTo(DisplayName);
        await Assert.That(configuration.ImagePath).IsEqualTo(ImagePath);
        await Assert.That(configuration.StartMode).IsEqualTo(ServiceStartMode.Automatic);
        await Assert.That(configuration.AccountName).IsEqualTo("LocalSystem");
        await Assert.That(configuration.Description).IsEqualTo(Description);
        await Assert.That(status.State).IsEqualTo(ServiceState.Running);
        await Assert.That(status.AcceptedControls)
            .IsEqualTo(ServiceAcceptedControls.Stop | ServiceAcceptedControls.PauseContinue);
        await Assert.That(status.ProcessId).IsEqualTo(ProcessIdentifier);
        await Assert.That(status.Checkpoint).IsEqualTo(Checkpoint);
        await Assert.That(status.WaitHintMilliseconds).IsEqualTo(WaitHint);
        await Assert.That(snapshot.Configuration).IsSameReferenceAs(configuration);
        await Assert.That(snapshot.Status).IsSameReferenceAs(status);
    }

    /// <summary>Verifies model defaults are safe.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Model_defaults_are_safe()
    {
        var definition = new ServiceDefinition(ServiceName, DisplayName, ExecutablePath);
        var update = new ServiceUpdate();
        var status = new ServiceStatus(ServiceState.Stopped);

        await Assert.That(definition.Arguments).IsEmpty();
        await Assert.That(definition.Dependencies).IsEmpty();
        await Assert.That(definition.StartMode).IsEqualTo(ServiceStartMode.Automatic);
        await Assert.That(definition.Description).IsNull();
        await Assert.That(definition.AccountName).IsNull();
        await Assert.That(definition.Password).IsNull();
        await Assert.That(update.DisplayName).IsNull();
        await Assert.That(update.ExecutablePath).IsNull();
        await Assert.That(update.Arguments).IsNull();
        await Assert.That(update.StartMode).IsNull();
        await Assert.That(update.Description).IsNull();
        await Assert.That(update.ChangeDescription).IsFalse();
        await Assert.That(update.AccountName).IsNull();
        await Assert.That(update.Password).IsNull();
        await Assert.That(update.ChangeAccount).IsFalse();
        await Assert.That(update.Dependencies).IsNull();
        await Assert.That(status.AcceptedControls).IsEqualTo(ServiceAcceptedControls.None);
        await Assert.That(status.ProcessId).IsEqualTo(0U);
        await Assert.That(status.Checkpoint).IsEqualTo(0U);
        await Assert.That(status.WaitHintMilliseconds).IsEqualTo(0U);
    }

    /// <summary>Verifies service-operation exception diagnostics.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Service_operation_exception_preserves_diagnostics()
    {
        var cause = new InvalidOperationException("cause");
        var empty = new ServiceOperationException();
        var message = new ServiceOperationException("message");
        var nested = new ServiceOperationException("nested", cause);
        var native = new ServiceOperationException("OpenService", ServiceName, NativeErrorCode, "denied");

        await Assert.That(empty.Message).IsEqualTo("A Windows service operation failed.");
        await Assert.That(empty.Operation).IsEmpty();
        await Assert.That(message.Message).IsEqualTo("message");
        await Assert.That(message.Operation).IsEmpty();
        await Assert.That(nested.InnerException).IsSameReferenceAs(cause);
        await Assert.That(nested.Operation).IsEmpty();
        await Assert.That(native.Operation).IsEqualTo("OpenService");
        await Assert.That(native.ServiceName).IsEqualTo(ServiceName);
        await Assert.That(native.NativeErrorCode).IsEqualTo(NativeErrorCode);
        await Assert.That(native.Message).IsEqualTo("denied");
    }

    /// <summary>Verifies reference and text guards.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Guards_validate_references_and_text()
    {
        var instance = new object();

        await Assert.That(Guard.NotNull(instance, GuardValue)).IsSameReferenceAs(instance);
        await Assert.That(static () => Guard.NotNull<object>(null, GuardValue)).Throws<ArgumentNullException>();
        await Assert.That(Guard.NotNullOrWhiteSpace(GuardValue, "text")).IsEqualTo(GuardValue);
        await Assert.That(static () => Guard.NotNullOrWhiteSpace(null, "text")).Throws<ArgumentNullException>();
        await Assert.That(static () => Guard.NotNullOrWhiteSpace(" ", "text")).Throws<ArgumentException>();
    }

    /// <summary>Verifies Windows argument escaping.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Windows_command_line_quotes_every_argument_shape()
    {
        var commandLine = WindowsCommandLine.Build(
            @"C:\Program Files\sample.exe",
            ["simple", string.Empty, "two words", "a\"b", @"trailing slash\"]);

        await Assert.That(commandLine)
            .IsEqualTo("\"C:\\Program Files\\sample.exe\" simple \"\" \"two words\" \"a\\\"b\" \"trailing slash\\\\\"");
        await Assert.That(WindowsCommandLine.Build("service.exe", [])).IsEqualTo("\"service.exe\"");
    }

    /// <summary>Verifies native dependency multi-string formatting.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Dependency_multi_strings_follow_native_contract()
    {
        await Assert.That(ServiceDependencies.ToMultiString(null)).IsNull();
        await Assert.That(ServiceDependencies.ToMultiString([])).IsEqualTo("\0");
        await Assert.That(ServiceDependencies.ToMultiString([DependencyName, "EventLog"]))
            .IsEqualTo($"{DependencyName}\0EventLog\0\0");
    }

    /// <summary>Verifies system adapter behavior.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task System_adapters_report_the_current_process_and_platform()
    {
        var path = new ApplicationPathResolver().Resolve();
        var suppliedPath = new ApplicationPathResolver(static () => ExecutablePath).Resolve();
        var fallbackPath = new ApplicationPathResolver(static () => null).Resolve();
        var platform = new SystemPlatform();
        var wait = new SystemWaitStrategy();
        wait.Delay(0);

        await Assert.That(path).IsNotEmpty();
        await Assert.That(suppliedPath).IsEqualTo(ExecutablePath);
        await Assert.That(fallbackPath).IsNotEmpty();
        await Assert.That(platform.IsWindows).IsTrue();
        await Assert.That(wait.ElapsedMilliseconds).IsGreaterThanOrEqualTo(0L);
        await Assert.That(static () => new ApplicationPathResolver(null!)).Throws<ArgumentNullException>();
    }
}
