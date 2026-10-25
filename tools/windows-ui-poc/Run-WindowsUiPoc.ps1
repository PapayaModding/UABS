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
. (Join-Path $PSScriptRoot 'ScenarioConfiguration.ps1')
try {
    $exitCode = Invoke-WithUabsScenario -ScenarioPath $scenarioPath -Action {
        param($temporaryScenario, $checkout)
        Write-Host "Testing v2-recovery commit $($checkout.commit) in $($checkout.repository)"
        try {
            $project = Join-Path $PSScriptRoot 'WindowsUiPoc.csproj'
            dotnet build $project -c Release --nologo | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'Runner build failed; no UI scenario executed.' }
            $start = [Diagnostics.ProcessStartInfo]::new('dotnet')
            $start.UseShellExecute = $false
            $start.ArgumentList.Add((Join-Path $PSScriptRoot 'bin/Release/net9.0-windows/WindowsUiPoc.dll'))
            $start.ArgumentList.Add($temporaryScenario)
            $start.ArgumentList.Add($outputPath)
            $process = [Diagnostics.Process]::Start($start)
            try {
                if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
                    # Only the runner and its own UABS child, never an existing user process.
                    $process.Kill($true)
                    $process.WaitForExit()
                    throw 'External watchdog expired; UIA provider may have stalled.'
                }
                if (-not (Test-Path -LiteralPath (Join-Path $outputPath 'results.json'))) {
                    throw "Runner exited without results (exit $($process.ExitCode))."
                }
                return $process.ExitCode
            }
            finally { $process.Dispose() }
        }
        finally {
            New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
            $checkout | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'checkout.json') -Encoding utf8NoBOM
        }
    }
}
catch {
    New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
    if (-not (Test-Path -LiteralPath (Join-Path $outputPath 'results.json'))) {
        @{ status = 'BLOCKED'; detail = $_.Exception.Message; completedUtc = [DateTime]::UtcNow } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'results.json') -Encoding utf8NoBOM
    }
    Write-Host "BLOCKED: $($_.Exception.Message)"
    $exitCode = 2
}
exit $exitCode
