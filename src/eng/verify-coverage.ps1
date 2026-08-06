param(
    [Parameter()]
    [string]$ResultsDirectory = (Join-Path $PSScriptRoot '..\TestResults')
)

$ErrorActionPreference = 'Stop'
$ExpectedModules = @('ServiceInstaller', 'ServiceInstaller.Cli')
$ResolvedResultsDirectory = Resolve-Path -LiteralPath $ResultsDirectory -ErrorAction Stop
$CoverageFiles = @(Get-ChildItem -LiteralPath $ResolvedResultsDirectory -Recurse -Filter '*.cobertura.xml' -File)
if ($CoverageFiles.Count -eq 0) {
    throw "No Cobertura reports were found under '$ResolvedResultsDirectory'."
}

$Observations = @{}
foreach ($CoverageFile in $CoverageFiles) {
    [xml]$Coverage = Get-Content -Raw -LiteralPath $CoverageFile.FullName
    foreach ($Package in @($Coverage.coverage.packages.package)) {
        $ModuleName = [string]$Package.name
        if ($ModuleName -notin $ExpectedModules) {
            continue
        }

        $Observations[$ModuleName] = [pscustomobject]@{
            File = $CoverageFile.FullName
            LineRate = [decimal]$Package.'line-rate'
            BranchRate = [decimal]$Package.'branch-rate'
        }
    }
}

$Failures = [System.Collections.Generic.List[string]]::new()
foreach ($ModuleName in $ExpectedModules) {
    if (-not $Observations.ContainsKey($ModuleName)) {
        $Failures.Add("Missing coverage module: $ModuleName")
        continue
    }

    $Observation = $Observations[$ModuleName]
    if ($Observation.LineRate -lt 1 -or $Observation.BranchRate -lt 1) {
        $Failures.Add(
            "$ModuleName is below 100% in '$($Observation.File)': " +
            "line=$($Observation.LineRate), branch=$($Observation.BranchRate)")
    }
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Coverage verification failed with $($Failures.Count) error(s)."
}

foreach ($ModuleName in $ExpectedModules) {
    Write-Host "${ModuleName}: 100% line / 100% branch"
}

Write-Host "Coverage verification passed for all $($ExpectedModules.Count) production modules."
