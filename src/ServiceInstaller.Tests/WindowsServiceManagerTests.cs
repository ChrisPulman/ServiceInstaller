// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using ServiceInstaller.Enums;

namespace ServiceInstaller.Tests;

/// <summary>Tests deterministic Windows Service Control Manager orchestration.</summary>
public sealed class WindowsServiceManagerTests
{
    /// <summary>The invalid start-mode value used by validation tests.</summary>
    private const ServiceStartMode InvalidStartMode = (ServiceStartMode)99;

    /// <summary>The short wait hint used by transition tests.</summary>
    private const uint ShortWaitHintMilliseconds = 200;

    /// <summary>The capped wait hint used by timeout tests.</summary>
    private const uint LongWaitHintMilliseconds = 50_000;

    /// <summary>The expected number of deletion calls in the multi-delete test.</summary>
    private const int ExpectedDeletionCount = 2;

    /// <summary>The executable name used by validation tests.</summary>
    private const string ExecutableName = "demo.exe";

    /// <summary>The one hundred millisecond transition timeout.</summary>
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(100);

    /// <summary>The default transition timeout.</summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The unsupported long timeout.</summary>
    private static readonly TimeSpan UnsupportedTimeout = TimeSpan.FromDays(100);

    /// <summary>Verifies constructor dependency guards.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ConstructorRejectsNullDependencies()
    {
        var wait = new FakeWaitStrategy();
        var platform = new FakePlatform();
        var native = new FakeNativeServiceApi();

        await Assert.That(() => new WindowsServiceManager(null!, wait, platform)).Throws<ArgumentNullException>();
        await Assert.That(() => new WindowsServiceManager(native, null!, platform)).Throws<ArgumentNullException>();
        await Assert.That(() => new WindowsServiceManager(native, wait, null!)).Throws<ArgumentNullException>();
    }

    /// <summary>Verifies existence checks use the connection-only SCM access.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ExistsReportsPresenceAndAbsence()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api, machineName: "server01");

        await Assert.That(manager.Exists("demo")).IsTrue();
        await Assert.That(api.MachineName).IsEqualTo("server01");
        await Assert.That(api.ManagerAccess).IsEqualTo(ScmAccessRights.Connect);
        await Assert.That(api.ServiceAccess).IsEqualTo(ServiceAccessRights.QueryStatus);
        await Assert.That(api.ManagerHandles[0].IsDisposed).IsTrue();
        await Assert.That(api.ServiceHandles[0].IsDisposed).IsTrue();

        api.ServiceExists = false;

        await Assert.That(manager.Exists("demo")).IsFalse();
    }

    /// <summary>Verifies creation escapes the image path, dependencies, and optional description.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task CreateSendsDefinitionToNativeApi()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);
        var definition = new ServiceDefinition("demo", "Demo", "C:\\Program Files\\demo.exe")
        {
            Arguments = ["--name", "a value"],
            Dependencies = ["Tcpip", "EventLog"],
            Description = "Demo service",
            StartMode = ServiceStartMode.Manual,
        };

        manager.Create(definition);

        await Assert.That(api.CreatedDefinition).IsSameReferenceAs(definition);
        await Assert.That(api.CreatedImagePath).IsEqualTo("\"C:\\Program Files\\demo.exe\" --name \"a value\"");
        await Assert.That(api.CreatedDependencies).IsEqualTo("Tcpip\0EventLog\0\0");
        await Assert.That(api.ManagerAccess).IsEqualTo(ScmAccessRights.Connect | ScmAccessRights.CreateService);
        await Assert.That(api.Descriptions.Count).IsEqualTo(1);
        await Assert.That(api.Descriptions[0]).IsEqualTo("Demo service");
        await Assert.That(api.ManagerHandles[0].IsDisposed).IsTrue();
        await Assert.That(api.ServiceHandles[0].IsDisposed).IsTrue();
    }

    /// <summary>Verifies creation does not write an unspecified description.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task CreateSkipsUnspecifiedDescription()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);

        var definition = new ServiceDefinition("demo", "Demo", ExecutableName);

        manager.Create(definition);

        await Assert.That(api.Descriptions).IsEmpty();
        await Assert.That(api.CreatedDependencies).IsEqualTo("\0");
    }

    /// <summary>Verifies creation validates all supported inputs before native calls.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task CreateValidatesInputs()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);

        await Assert.That(() => manager.Create(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => manager.Create(new(" ", "Demo", ExecutableName))).Throws<ArgumentException>();
        await Assert.That(() => manager.Create(new("demo", " ", ExecutableName))).Throws<ArgumentException>();
        await Assert.That(() => manager.Create(new("demo", "Demo", " "))).Throws<ArgumentException>();
        await Assert.That(() => manager.Create(new("demo", "Demo", ExecutableName) { StartMode = InvalidStartMode }))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(api.ManagerHandles).IsEmpty();
    }

    /// <summary>Verifies configuration replacement values and description updates are forwarded.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ConfigureSendsReplacementValues()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);
        var update = new ServiceUpdate
        {
            ExecutablePath = "C:\\Program Files\\updated.exe",
            Arguments = ["--value", "with spaces"],
            Dependencies = ["Tcpip"],
            ChangeDescription = true,
            Description = string.Empty,
            StartMode = ServiceStartMode.Disabled,
        };

        manager.Configure("demo", update);

        await Assert.That(api.ChangedUpdate).IsSameReferenceAs(update);
        await Assert.That(api.ChangedImagePath).IsEqualTo("\"C:\\Program Files\\updated.exe\" --value \"with spaces\"");
        await Assert.That(api.ChangedDependencies).IsEqualTo("Tcpip\0\0");
        await Assert.That(api.Descriptions.Count).IsEqualTo(1);
        await Assert.That(api.Descriptions[0]).IsEqualTo(string.Empty);
        await Assert.That(api.ServiceAccess).IsEqualTo(ServiceAccessRights.ChangeConfig);

        var executableOnly = new ServiceUpdate { ExecutablePath = ExecutableName };
        manager.Configure("demo", executableOnly);

        await Assert.That(api.ChangedImagePath).IsEqualTo($"\"{ExecutableName}\"");
    }

    /// <summary>Verifies configuration validation and missing-service handling.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ConfigureRejectsInvalidUpdatesAndMissingServices()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);

        await Assert.That(() => manager.Configure(" ", new())).Throws<ArgumentException>();
        await Assert.That(() => manager.Configure("demo", null!)).Throws<ArgumentNullException>();
        await Assert.That(() => manager.Configure("demo", new() { StartMode = InvalidStartMode }))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => manager.Configure("demo", new() { ExecutablePath = " " })).Throws<ArgumentException>();
        await Assert.That(() => manager.Configure("demo", new() { Arguments = ["value"] })).Throws<ArgumentException>();

        api.ServiceExists = false;

        await Assert.That(() => manager.Configure("demo", new())).Throws<ServiceOperationException>();
    }

    /// <summary>Verifies queries combine the native configuration and status results.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task QueryReturnsSnapshotAndReportsMissingServices()
    {
        var api = new FakeNativeServiceApi();
        var expectedStatus = Status(ServiceState.Running);
        api.Statuses.Enqueue(expectedStatus);
        var manager = CreateManager(api);

        var snapshot = manager.Query("demo");

        await Assert.That(snapshot.Configuration).IsSameReferenceAs(api.Configuration);
        await Assert.That(snapshot.Status).IsSameReferenceAs(expectedStatus);
        await Assert.That(api.ServiceAccess).IsEqualTo(ServiceAccessRights.QueryStatus | ServiceAccessRights.QueryConfig);

        api.ServiceExists = false;

        await Assert.That(() => manager.Query("demo")).Throws<ServiceOperationException>();
    }

    /// <summary>Verifies start handles already running, pending, paused, and stopped services.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task StartHandlesSupportedStates()
    {
        var api = new FakeNativeServiceApi();
        var wait = new FakeWaitStrategy();
        var manager = CreateManager(api, wait);

        var running = Status(ServiceState.Running);
        api.Statuses.Enqueue(running);

        await Assert.That(manager.Start("demo", TimeSpan.FromSeconds(1))).IsSameReferenceAs(running);
    }

    /// <summary>Verifies start waits for an already-pending service.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task StartWaitsForPendingService()
    {
        var api = new FakeNativeServiceApi();
        var wait = new FakeWaitStrategy();
        var manager = CreateManager(api, wait);
        var running = Status(ServiceState.Running);
        api.Statuses.Enqueue(Status(ServiceState.StartPending, ShortWaitHintMilliseconds));
        api.Statuses.Enqueue(Status(ServiceState.StartPending, ShortWaitHintMilliseconds));
        api.Statuses.Enqueue(running);

        var result = manager.Start("demo", TimeSpan.FromSeconds(1));

        await Assert.That(result).IsSameReferenceAs(running);
        await Assert.That(wait.Delays).IsEquivalentTo([(int)ShortTimeout.TotalMilliseconds]);
        await Assert.That(api.StartArguments).IsEmpty();
    }

    /// <summary>Verifies start continues a paused service and starts a stopped service.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task StartContinuesPausedAndStartsStoppedServices()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);
        api.Statuses.Enqueue(Status(ServiceState.Paused));
        api.Statuses.Enqueue(Status(ServiceState.ContinuePending));
        api.Statuses.Enqueue(Status(ServiceState.Running));

        _ = manager.Start("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEquivalentTo([ServiceControl.Continue]);

        api.Statuses.Enqueue(Status(ServiceState.Stopped));
        api.Statuses.Enqueue(Status(ServiceState.StartPending));
        api.Statuses.Enqueue(Status(ServiceState.Running));

        _ = manager.Start("demo", TimeSpan.FromSeconds(1), "one", "two");

        await Assert.That(api.StartArguments[0]).IsEquivalentTo(["one", "two"]);
        await Assert.That(api.ServiceAccess)
            .IsEqualTo(ServiceAccessRights.QueryStatus | ServiceAccessRights.Start | ServiceAccessRights.PauseContinue);

        api.Statuses.Enqueue(Status(ServiceState.Stopped));
        api.Statuses.Enqueue(Status(ServiceState.StartPending));
        api.Statuses.Enqueue(Status(ServiceState.Running));

        _ = manager.Start("demo", TimeSpan.FromSeconds(1), null!);

        await Assert.That(api.StartArguments[1]).IsEmpty();
    }

    /// <summary>Verifies start rejects a state that cannot become running.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task StartRejectsInvalidState()
    {
        var api = new FakeNativeServiceApi();
        api.Statuses.Enqueue(Status(ServiceState.StopPending));
        var manager = CreateManager(api);

        await Assert.That(() => manager.Start("demo", TimeSpan.FromSeconds(1))).Throws<InvalidOperationException>();
    }

    /// <summary>Verifies lifecycle controls handle final, pending, and valid source states.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ControlsHandleValidStates()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);
        api.Statuses.Enqueue(Status(ServiceState.Stopped));

        _ = manager.Stop("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEmpty();

        api.Statuses.Enqueue(Status(ServiceState.Running));
        api.Statuses.Enqueue(Status(ServiceState.PausePending));
        api.Statuses.Enqueue(Status(ServiceState.Paused));

        _ = manager.Pause("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEquivalentTo([ServiceControl.Pause]);

        api.Statuses.Enqueue(Status(ServiceState.ContinuePending));
        api.Statuses.Enqueue(Status(ServiceState.Running));

        _ = manager.Continue("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEquivalentTo([ServiceControl.Pause]);

        api.Statuses.Enqueue(Status(ServiceState.Paused));
        api.Statuses.Enqueue(Status(ServiceState.ContinuePending));
        api.Statuses.Enqueue(Status(ServiceState.Running));

        _ = manager.Continue("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEquivalentTo([ServiceControl.Pause, ServiceControl.Continue]);
    }

    /// <summary>Verifies lifecycle controls wait when already pending and reject invalid source states.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ControlsWaitAndRejectInvalidState()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);
        api.Statuses.Enqueue(Status(ServiceState.StopPending));
        api.Statuses.Enqueue(Status(ServiceState.Stopped));

        _ = manager.Stop("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEmpty();

        api.Statuses.Enqueue(Status(ServiceState.Stopped));

        await Assert.That(() => manager.Pause("demo", TimeSpan.FromSeconds(1))).Throws<InvalidOperationException>();
        await Assert.That(api.ServiceAccess).IsEqualTo(ServiceAccessRights.QueryStatus | ServiceAccessRights.PauseContinue);
    }

    /// <summary>Verifies deletion returns false for missing services and deletes stopped services.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task DeleteHandlesMissingAndStoppedServices()
    {
        var api = new FakeNativeServiceApi { ServiceExists = false };
        var manager = CreateManager(api);

        await Assert.That(manager.Delete("demo", TimeSpan.FromSeconds(1))).IsFalse();

        api.ServiceExists = true;
        api.Statuses.Enqueue(Status(ServiceState.Stopped));

        await Assert.That(manager.Delete("demo", TimeSpan.FromSeconds(1))).IsTrue();
        await Assert.That(api.DeleteCount).IsEqualTo(1);
        await Assert.That(api.Controls).IsEmpty();
    }

    /// <summary>Verifies deletion waits for a pending stop and stops running services first.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task DeleteStopsAndWaitsWhenRequired()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);
        api.Statuses.Enqueue(Status(ServiceState.StopPending));
        api.Statuses.Enqueue(Status(ServiceState.Stopped));

        _ = manager.Delete("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEmpty();

        api.Statuses.Enqueue(Status(ServiceState.Running));
        api.Statuses.Enqueue(Status(ServiceState.StopPending));
        api.Statuses.Enqueue(Status(ServiceState.Stopped));

        _ = manager.Delete("demo", TimeSpan.FromSeconds(1));

        await Assert.That(api.Controls).IsEquivalentTo([ServiceControl.Stop]);
        await Assert.That(api.DeleteCount).IsEqualTo(ExpectedDeletionCount);
        await Assert.That(api.ServiceAccess)
            .IsEqualTo(ServiceAccessRights.QueryStatus | ServiceAccessRights.Stop | ServiceAccessRights.Delete);
    }

    /// <summary>Verifies transition failures and timeouts are surfaced.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task LifecycleReportsTransitionFailuresAndTimeouts()
    {
        var failedApi = new FakeNativeServiceApi();
        failedApi.Statuses.Enqueue(Status(ServiceState.Running));
        failedApi.Statuses.Enqueue(Status(ServiceState.StartPending));
        failedApi.Statuses.Enqueue(Status(ServiceState.Stopped));
        var failedManager = CreateManager(failedApi);

        await Assert.That(() => failedManager.Stop("demo", TimeSpan.FromSeconds(1))).Throws<InvalidOperationException>();

        var timeoutApi = new FakeNativeServiceApi();
        var timeoutWait = new FakeWaitStrategy();
        timeoutApi.Statuses.Enqueue(Status(ServiceState.Running));
        timeoutApi.Statuses.Enqueue(Status(ServiceState.StopPending, LongWaitHintMilliseconds));
        timeoutApi.Statuses.Enqueue(Status(ServiceState.StopPending, LongWaitHintMilliseconds));
        var timeoutManager = CreateManager(timeoutApi, timeoutWait);

        await Assert.That(() => timeoutManager.Stop("demo", ShortTimeout)).Throws<TimeoutException>();
        await Assert.That(timeoutWait.Delays).IsEquivalentTo([(int)ShortTimeout.TotalMilliseconds]);
    }

    /// <summary>Verifies validation and platform checks prevent unsupported native calls.</summary>
    /// <returns>A task that represents the test.</returns>
    [Test]
    public async Task ValidatesTimeoutsAndPlatform()
    {
        var api = new FakeNativeServiceApi();
        var manager = CreateManager(api);

        await Assert.That(() => manager.Start(" ", TimeSpan.FromSeconds(1))).Throws<ArgumentException>();
        await Assert.That(() => manager.Stop("demo", TimeSpan.Zero)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => manager.Stop("demo", UnsupportedTimeout)).Throws<ArgumentOutOfRangeException>();

        var unsupportedPlatform = new FakePlatform(false);
        var unsupported = CreateManager(api, platform: unsupportedPlatform);

        await Assert.That(() => unsupported.Exists("demo")).Throws<PlatformNotSupportedException>();
        await Assert.That(api.ManagerHandles).IsEmpty();
        await Assert.That(WindowsServiceManager.GetDefaultTimeout()).IsEqualTo(DefaultTimeout);
    }

    /// <summary>Creates a manager with deterministic collaborating objects.</summary>
    /// <param name="api">The fake native API.</param>
    /// <param name="wait">The optional fake wait strategy.</param>
    /// <param name="platform">The optional fake platform.</param>
    /// <param name="machineName">The optional machine name.</param>
    /// <returns>The configured manager.</returns>
    private static WindowsServiceManager CreateManager(
        FakeNativeServiceApi api,
        FakeWaitStrategy? wait = null,
        FakePlatform? platform = null,
        string? machineName = null) =>
        new(api, wait ?? new(), platform ?? new(), machineName);

    /// <summary>Creates a service status with an optional native wait hint.</summary>
    /// <param name="state">The service state.</param>
    /// <param name="waitHintMilliseconds">The wait hint in milliseconds.</param>
    /// <returns>The configured status.</returns>
    private static ServiceStatus Status(ServiceState state, uint waitHintMilliseconds = 1000) =>
        new(state, ServiceAcceptedControls.None, 0, 0, waitHintMilliseconds);
}
