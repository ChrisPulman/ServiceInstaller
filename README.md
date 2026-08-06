# ServiceInstaller

ServiceInstaller is a composable Windows Service Control Manager library with a terminal CLI. It can create, configure, query, start, stop, pause, continue, and delete services on the local computer or a remote Windows computer.

The executable you register must already implement the Windows Service hosting contract, for example with `Microsoft.Extensions.Hosting.WindowsServices`. Registering an ordinary console program does not turn it into a service.

## Packages

```powershell
dotnet add package ServiceInstaller
dotnet tool install --global ServiceInstaller.Cli
```

Creating, changing, and deleting services normally requires an elevated terminal. Remote control also requires Service Control Manager access on the target computer.

## Terminal CLI

```text
serviceinstaller create --name SampleService --display-name "Sample Service" --binary "C:\Services\Sample.exe" --description "Example worker" --start-mode automatic --start -- --service
serviceinstaller configure --name SampleService --binary "C:\Services\Sample.exe" --start-mode manual -- --service --reconfigured
serviceinstaller status --name SampleService
serviceinstaller status --name SampleService --json
serviceinstaller exists --name SampleService
serviceinstaller start --name SampleService --timeout 45 -- runtime-argument
serviceinstaller pause --name SampleService
serviceinstaller continue --name SampleService
serviceinstaller stop --name SampleService
serviceinstaller delete --name SampleService
```

Use `--machine SERVER01` with any command to target a remote computer. `install`, `query`, `resume`, and `uninstall` are aliases for `create` with immediate start, `status`, `continue`, and `delete` respectively.

Account passwords are accepted only through standard input:

```powershell
$password | serviceinstaller create --name SampleService --display-name "Sample Service" --binary "C:\Services\Sample.exe" --account ".\ServiceUser" --password-stdin
```

The process returns stable exit codes: `0` for success, `2` for invalid arguments, `3` for an operation failure, and `4` for an unsupported platform or access denial.

## Library API

```csharp
using ServiceInstaller;
using ServiceInstaller.Enums;

IServiceManager services = new WindowsServiceManager();

services.Create(new ServiceDefinition(
    "SampleService",
    "Sample Service",
    @"C:\Services\Sample.exe")
{
    Arguments = ["--service"],
    Description = "Example worker",
    StartMode = ServiceStartMode.Automatic,
    Dependencies = ["RpcSs"],
});

var running = services.Start("SampleService", TimeSpan.FromSeconds(30));
var snapshot = services.Query("SampleService");

services.Configure("SampleService", new ServiceUpdate
{
    DisplayName = "Updated Sample Service",
    Description = "Updated description",
    ChangeDescription = true,
    StartMode = ServiceStartMode.Manual,
});

services.Stop("SampleService", TimeSpan.FromSeconds(30));
services.Delete("SampleService", TimeSpan.FromSeconds(30));
```

`WindowsServiceManager` owns native handles safely and exposes its operating-system boundary through composition, allowing service lifecycle behavior to be tested without installing real services. The original `ServiceController.HandleRequest` API remains available as a compatibility adapter.

## Build and test

The launchers bootstrap the SDK pinned by `global.json` when it is not installed globally, then run the Nuke build:

```powershell
./build.ps1 Compile --configuration Release
```

On Command Prompt or Unix-like shells use `build.cmd` or `./build.sh`. Tests run on Microsoft Testing Platform with TUnit, and CI rejects production line or branch coverage below 100%.

The repository includes two GitHub Actions workflows:

- `BuildOnly.yml` compiles and verifies tests and coverage for branches and pull requests.
- `BuildDeploy.yml` performs a manually selected semantic version bump, validates the release, packs both NuGet packages, publishes them, and creates a GitHub release.

`BuildDeploy.yml` expects a protected `release` environment and a `NUGET_API_KEY` secret.
