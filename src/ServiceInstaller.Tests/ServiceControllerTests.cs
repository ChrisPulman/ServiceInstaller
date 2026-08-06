// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;
namespace ServiceInstaller.Tests;

/// <summary>Tests the legacy service-controller compatibility adapter.</summary>
public sealed class ServiceControllerTests
{
    /// <summary>The expected default transition timeout.</summary>
    private const int DefaultTimeoutSeconds = 30;

    /// <summary>The sample display name.</summary>
    private const string DisplayName = "Sample";

    /// <summary>The sample executable path.</summary>
    private const string ExecutablePath = "sample.exe";

    /// <summary>The sample stable service name.</summary>
    private const string ServiceName = "sample";

    /// <summary>The legacy start command.</summary>
    private const string StartCommand = "-start";

    /// <summary>The deterministic application path resolver.</summary>
    private static readonly FakeApplicationPathResolver PathResolver = new(ExecutablePath);

    /// <summary>Verifies installation creates and starts a missing service.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Legacy_install_creates_a_missing_service_and_starts_it()
    {
        var manager = new RecordingServiceManager();

        var result = ServiceController.HandleRequest(
            " -INSTALL ",
            ServiceName,
            DisplayName,
            manager,
            PathResolver,
            ["--service"]);

        await Assert.That(result).IsEqualTo("Service installed and started successfully");
        await Assert.That(manager.Operations).IsEquivalentTo(["exists:sample", "create:sample", "start:sample"]);
        await Assert.That(manager.Definition).IsNotNull();
        await Assert.That(manager.Definition!.ExecutablePath).IsEqualTo(ExecutablePath);
        await Assert.That(manager.Definition.Arguments).IsEquivalentTo(["--service"]);
        await Assert.That(manager.Timeout).IsEqualTo(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
    }

    /// <summary>Verifies installation reuses an existing service.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Legacy_install_reuses_an_existing_service()
    {
        var manager = new RecordingServiceManager { ExistsResult = true };

        var result = ServiceController.HandleRequest(
            "-install",
            ServiceName,
            DisplayName,
            manager,
            PathResolver,
            []);

        await Assert.That(result).IsEqualTo("Service installed and started successfully");
        await Assert.That(manager.Operations).IsEquivalentTo(["exists:sample", "start:sample"]);
        await Assert.That(manager.Definition).IsNull();
    }

    /// <summary>Verifies lifecycle commands delegate to the manager.</summary>
    /// <param name="command">The legacy command.</param>
    /// <param name="expected">The expected response.</param>
    /// <param name="operation">The expected recorded operation.</param>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    [Arguments("-start", "Service started successfully", "start:sample")]
    [Arguments("-stop", "Service stopped successfully", "stop:sample")]
    [Arguments("-pause", "Service paused successfully", "pause:sample")]
    [Arguments("-continue", "Service continued successfully", "continue:sample")]
    [Arguments("-resume", "Service continued successfully", "continue:sample")]
    public async Task Legacy_lifecycle_commands_delegate_to_the_manager(
        string command,
        string expected,
        string operation)
    {
        var manager = new RecordingServiceManager();

        var result = ServiceController.HandleRequest(command, ServiceName, DisplayName, manager, PathResolver, []);

        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(manager.Operations).IsEquivalentTo([operation]);
    }

    /// <summary>Verifies query and existence results are readable.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Legacy_query_and_existence_commands_return_human_readable_results()
    {
        var manager = new RecordingServiceManager { ExistsResult = true };

        var status = ServiceController.HandleRequest("-status", ServiceName, DisplayName, manager, PathResolver, []);
        var installed = ServiceController.HandleRequest("-isinstalled", ServiceName, DisplayName, manager, PathResolver, []);
        manager.ExistsResult = false;
        var missing = ServiceController.HandleRequest("-isinstalled", ServiceName, DisplayName, manager, PathResolver, []);

        await Assert.That(status).IsEqualTo(ServiceState.Running.ToString());
        await Assert.That(installed).IsEqualTo("The service is installed");
        await Assert.That(missing).IsEqualTo("The service is not installed");
    }

    /// <summary>Verifies uninstall reports deletion and absence.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Legacy_uninstall_reports_both_results()
    {
        var manager = new RecordingServiceManager();

        var deleted = ServiceController.HandleRequest("-uninstall", ServiceName, DisplayName, manager, PathResolver, []);
        manager.DeleteResult = false;
        var missing = ServiceController.HandleRequest("-uninstall", ServiceName, DisplayName, manager, PathResolver, []);

        await Assert.That(deleted).IsEqualTo("Service uninstalled successfully");
        await Assert.That(missing).IsEqualTo("Service is not installed");
    }

    /// <summary>Verifies custom application commands appear in help.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Legacy_help_includes_application_commands_when_configured()
    {
        ServiceController.AddApplicationArguments(null!);
        var plain = ServiceController.HandleRequest(null, ServiceName, DisplayName, new RecordingServiceManager(), PathResolver, []);
        ServiceController.AddApplicationArguments("-Custom");
        var extended = ServiceController.HandleRequest("unknown", ServiceName, DisplayName, new RecordingServiceManager(), PathResolver, []);
        ServiceController.AddApplicationArguments();

        await Assert.That(plain).Contains("Valid Service Arguments are:");
        await Assert.That(plain).DoesNotContain("Additional Arguments:");
        await Assert.That(extended).Contains("Additional Arguments:");
        await Assert.That(extended).Contains("-Custom");
    }

    /// <summary>Verifies failures are sanitized and dependencies are validated.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Legacy_adapter_returns_safe_failures_and_validates_dependencies()
    {
        var manager = new RecordingServiceManager { Failure = new InvalidOperationException("failure") };

        var result = ServiceController.HandleRequest(StartCommand, ServiceName, DisplayName, manager, PathResolver, []);

        await Assert.That(result).IsEqualTo("Service request failed: failure");
        await Assert.That(static () => ServiceController.HandleRequest(
            StartCommand,
            ServiceName,
            DisplayName,
            null!,
            PathResolver,
            [])).Throws<ArgumentNullException>();
        await Assert.That(static () => ServiceController.HandleRequest(
            StartCommand,
            ServiceName,
            DisplayName,
            new RecordingServiceManager(),
            null!,
            [])).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies public help avoids native calls.</summary>
    /// <returns>A task that completes when assertions finish.</returns>
    [Test]
    public async Task Public_legacy_help_does_not_touch_the_native_API()
    {
        var result = ServiceController.HandleRequest(null, ServiceName, DisplayName, null!);

        await Assert.That(result).Contains("-Install");
    }
}
