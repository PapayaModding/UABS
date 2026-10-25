#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../ScenarioConfiguration.ps1')
function Assert-Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-Rejected([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Assert-Check $rejected 'Expected preflight rejection.'
}

$tokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot '../Run-WindowsUiPoc.ps1'), [ref]$tokens, [ref]$parseErrors)
Assert-Check ($parseErrors.Count -eq 0) 'Launcher contains PowerShell syntax errors.'

$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('uabs-scenario-checks-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $script:fakeRoot = $testRoot
    $script:fakeBranch = 'v2-recovery'
    $script:fakeCommit = 'a' * 40
    $script:gitFails = $false
    $script:detached = $false
    $fakeGit = {
        param($repository, $arguments)
        if ($script:gitFails) { throw 'Git unavailable or command failed.' }
        switch ($arguments[0]) {
            'symbolic-ref' {
                if ($script:detached) { throw 'fatal: ref HEAD is not a symbolic ref' }
                return $script:fakeBranch
            }
            'rev-parse' {
                if ($arguments[1] -eq '--show-toplevel') { return $script:fakeRoot }
                return $script:fakeCommit
            }
            default { throw 'Unexpected Git query.' }
        }
    }
    $checkout = Resolve-UabsCheckout -Repository $testRoot -GitRunner $fakeGit
    Assert-Check ($checkout.commit -ceq ('a' * 40)) 'Valid HEAD did not resolve.'
    foreach ($branch in @('main', 'V2-recovery', '', "v2-recovery`nextra")) {
        $script:fakeBranch = $branch
        Assert-Rejected { Resolve-UabsCheckout -Repository $testRoot -GitRunner $fakeGit }
    }
    $script:fakeBranch = 'v2-recovery'
    $script:detached = $true
    Assert-Rejected { Resolve-UabsCheckout -Repository $testRoot -GitRunner $fakeGit }
    $script:detached = $false
    foreach ($sha in @('', 'abc1234', ('z' * 40), (('a' * 40) + "`nextra"))) {
        $script:fakeCommit = $sha
        Assert-Rejected { Resolve-UabsCheckout -Repository $testRoot -GitRunner $fakeGit }
    }
    $script:fakeCommit = 'a' * 40
    $script:gitFails = $true
    Assert-Rejected { Resolve-UabsCheckout -Repository $testRoot -GitRunner $fakeGit }
    $script:gitFails = $false
    Assert-Rejected { Resolve-UabsCheckout -Repository (Join-Path $testRoot 'missing') -GitRunner $fakeGit }
    $script:fakeRoot = Join-Path $testRoot 'different-root'
    Assert-Rejected { Resolve-UabsCheckout -Repository $testRoot -GitRunner $fakeGit }
    $script:fakeRoot = $testRoot

    $scenarioPath = Join-Path $testRoot 'scenario.local.json'
    $original = @{
        repository = $testRoot; expectedCommit = 'old value'; package = 'disposable.uabs.sqlite'
        packageSha256 = 'b' * 64; keyword = 'テスト'
        expectedRows = @(@{ name = 'テスト.bundle'; package = 'Smoke' })
        extra = @{ nested = @(1, 2, 3) }
    }
    $original | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $scenarioPath -Encoding utf8NoBOM
    $before = [IO.File]::ReadAllBytes($scenarioPath)
    $script:temporaryPath = $null
    Invoke-WithUabsScenario -ScenarioPath $scenarioPath -GitRunner $fakeGit -Action {
        param($temporary, $verified)
        $script:temporaryPath = $temporary
        Assert-Check (Test-Path -LiteralPath $temporary) 'Temporary scenario not created.'
        $updated = Get-Content -LiteralPath $temporary -Raw | ConvertFrom-Json -AsHashtable
        Assert-Check ($updated.expectedCommit -ceq $verified.commit) 'Commit not populated.'
        $updated.Remove('expectedCommit')
        $reference = Get-Content -LiteralPath $scenarioPath -Raw | ConvertFrom-Json -AsHashtable
        $reference.Remove('expectedCommit')
        Assert-Check (($updated | ConvertTo-Json -Depth 100 -Compress) -ceq ($reference | ConvertTo-Json -Depth 100 -Compress)) 'Other scenario values changed.'
    }
    Assert-Check (-not (Test-Path -LiteralPath $script:temporaryPath)) 'Temporary scenario remained after success.'
    Assert-Rejected {
        Invoke-WithUabsScenario -ScenarioPath $scenarioPath -GitRunner $fakeGit -Action {
            param($temporary, $verified)
            $script:temporaryPath = $temporary
            throw 'Simulated build, runner startup, or watchdog failure.'
        }
    }
    Assert-Check (-not (Test-Path -LiteralPath $script:temporaryPath)) 'Temporary scenario remained after failure.'
    $code = Invoke-WithUabsScenario -ScenarioPath $scenarioPath -GitRunner $fakeGit -Action {
        param($temporary, $verified)
        $script:temporaryPath = $temporary
        return 2
    }
    Assert-Check ($code -eq 2 -and -not (Test-Path -LiteralPath $script:temporaryPath)) 'Nonzero runner result or cleanup was lost.'
    Assert-Check ([Convert]::ToBase64String($before) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes($scenarioPath))) 'Original scenario was modified.'
    Write-Host 'PASS: valid HEAD, branch/detached/invalid Git/path rejection, scenario preservation, and success/failure cleanup.'
}
finally { Remove-Item -LiteralPath $testRoot -Recurse -Force }
