#Requires -Version 7.0
# Functions only: safe to load for regression checks without launching UABS.
function Invoke-UabsGit {
    param([string]$Repository, [string[]]$Arguments)
    $git = Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1
    $start = [Diagnostics.ProcessStartInfo]::new($git.Source)
    $start.WorkingDirectory = $Repository
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(10000)) {
            $process.Kill($true)
            throw 'Git preflight timed out.'
        }
        if ($process.ExitCode -ne 0) { throw "Git preflight failed: $($stderr.GetAwaiter().GetResult())" }
        return $stdout.GetAwaiter().GetResult().Trim()
    }
    finally { $process.Dispose() }
}

function Resolve-UabsCheckout {
    param(
        [string]$Repository,
        [scriptblock]$GitRunner = { param($root, $gitArguments) Invoke-UabsGit -Repository $root -Arguments $gitArguments }
    )
    if ([string]::IsNullOrWhiteSpace($Repository) -or -not [IO.Path]::IsPathFullyQualified($Repository) -or
        -not (Test-Path -LiteralPath $Repository -PathType Container)) {
        throw 'Scenario repository must be an existing absolute directory.'
    }
    $resolved = Resolve-Path -LiteralPath $Repository -ErrorAction Stop
    if ($resolved.Provider.Name -ne 'FileSystem') { throw 'Repository must use the filesystem provider.' }
    $root = [IO.Path]::TrimEndingDirectorySeparator($resolved.ProviderPath)
    $top = & $GitRunner $root @('rev-parse', '--show-toplevel')
    if ($top -isnot [string] -or -not [IO.Path]::IsPathFullyQualified($top) -or
        -not [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($top)).Equals($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Configured repository must be the exact Git working-tree root; no repository substitution is allowed.'
    }
    # symbolic-ref fails for detached HEAD; do not infer a branch from remote refs.
    $branch = & $GitRunner $root @('symbolic-ref', '--quiet', '--short', 'HEAD')
    if ($branch -isnot [string] -or $branch -cne 'v2-recovery') { throw 'An attached v2-recovery branch is required.' }
    $commit = & $GitRunner $root @('rev-parse', '--verify', 'HEAD^{commit}')
    if ($commit -isnot [string] -or $commit -cnotmatch '\A[0-9a-fA-F]{40}\z') { throw 'Git did not resolve a full 40-character HEAD commit SHA.' }
    return @{ repository = $root; branch = $branch; commit = $commit.ToLowerInvariant() }
}

function Invoke-WithUabsScenario {
    param(
        [Parameter(Mandatory)][string]$ScenarioPath,
        [Parameter(Mandatory)][scriptblock]$Action,
        [scriptblock]$GitRunner = { param($root, $gitArguments) Invoke-UabsGit -Repository $root -Arguments $gitArguments }
    )
    $scenario = Get-Content -LiteralPath $ScenarioPath -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable -Depth 100
    if ($scenario -isnot [System.Collections.IDictionary]) { throw 'Scenario JSON must be an object.' }
    $checkout = Resolve-UabsCheckout -Repository $scenario['repository'] -GitRunner $GitRunner
    $scenario['expectedCommit'] = $checkout.commit
    $temporary = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($temporary, ($scenario | ConvertTo-Json -Depth 100), [Text.UTF8Encoding]::new($false))
        & $Action $temporary $checkout
    }
    finally { Remove-Item -LiteralPath $temporary -Force -ErrorAction Stop }
}
