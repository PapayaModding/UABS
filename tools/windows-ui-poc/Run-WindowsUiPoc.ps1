#Requires -Version 7.0
param(
    [Parameter(Mandatory)][string]$Scenario,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "artifacts/$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0,8))"),
    [ValidateRange(60,600)][int]$TimeoutSeconds = 180
)
$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'This runner requires Windows and an unlocked interactive desktop.' }
$scenarioPath = (Resolve-Path -LiteralPath $Scenario).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) { throw 'OutputDirectory must not already exist.' }
$project = Join-Path $PSScriptRoot 'WindowsUiPoc.csproj'
dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Runner build failed; no UI scenario executed.' }
$start = [Diagnostics.ProcessStartInfo]::new('dotnet')
$start.UseShellExecute = $false
$start.ArgumentList.Add((Join-Path $PSScriptRoot 'bin/Release/net9.0-windows/WindowsUiPoc.dll'))
$start.ArgumentList.Add($scenarioPath)
$start.ArgumentList.Add($outputPath)
$process = [Diagnostics.Process]::Start($start)
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    # Kill only the runner and its own UABS child, never an existing user process.
    $process.Kill($true)
    $process.WaitForExit()
    New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
    @{ status = 'BLOCKED'; detail = 'External watchdog expired; UIA provider may have stalled.'; completedUtc = [DateTime]::UtcNow } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'results.json')
    exit 2
}
if (-not (Test-Path -LiteralPath (Join-Path $outputPath 'results.json'))) {
    New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
    @{ status = 'BLOCKED'; detail = "Runner exited without results (exit $($process.ExitCode))."; completedUtc = [DateTime]::UtcNow } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'results.json')
    exit 2
}
exit $process.ExitCode
