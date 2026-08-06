[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]]$BuildArguments
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$ConfirmPreference = 'None'

$RepositoryRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$BuildProjectFile = Join-Path $RepositoryRoot 'build\_build.csproj'
$TempDirectory = Join-Path $RepositoryRoot '.nuke\temp'
$DotNetGlobalFile = Join-Path $RepositoryRoot 'global.json'
$DotNetInstallFile = Join-Path $TempDirectory 'dotnet-install.ps1'
$DotNetDirectory = Join-Path $TempDirectory 'dotnet-win'
$DotNetInstallUrl = 'https://dot.net/v1/dotnet-install.ps1'

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
$env:NUKE_TELEMETRY_OPTOUT = 'true'

function Invoke-Checked([scriptblock]$Command) {
    & $Command
    if ($LASTEXITCODE) {
        exit $LASTEXITCODE
    }
}

$GlobalDotNet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
$GlobalDotNetExitCode = 1
if ($null -ne $GlobalDotNet) {
    $PreviousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    & $GlobalDotNet.Path --version *> $null
    $GlobalDotNetExitCode = $LASTEXITCODE
    $ErrorActionPreference = $PreviousErrorActionPreference
}

if ($null -ne $GlobalDotNet -and $GlobalDotNetExitCode -eq 0) {
    $env:DOTNET_EXE = $GlobalDotNet.Path
}
else {
    New-Item -ItemType Directory -Path $TempDirectory -Force | Out-Null
    if (-not (Test-Path -LiteralPath $DotNetInstallFile)) {
        Invoke-WebRequest -Uri $DotNetInstallUrl -OutFile $DotNetInstallFile -UseBasicParsing
    }

    $DotNetGlobal = Get-Content -LiteralPath $DotNetGlobalFile -Raw | ConvertFrom-Json
    $DotNetVersion = $DotNetGlobal.sdk.version
    $LocalDotNet = Join-Path $DotNetDirectory 'dotnet.exe'
    if (-not (Test-Path -LiteralPath $LocalDotNet) -or (& $LocalDotNet --version) -ne $DotNetVersion) {
        Invoke-Checked {
            & powershell -ExecutionPolicy ByPass -NoProfile -File $DotNetInstallFile `
                -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath
        }
    }

    $env:DOTNET_EXE = $LocalDotNet
}

Write-Output "Microsoft .NET SDK version $(& $env:DOTNET_EXE --version)"
Invoke-Checked {
    & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false `
        /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
}
Invoke-Checked {
    & $env:DOTNET_EXE run --project $BuildProjectFile --no-build -- $BuildArguments
}
