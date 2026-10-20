# Run after a benchmark has completed, with its disposable artifact directory.
param([Parameter(Mandatory=$true)][string]$ArtifactDirectory)
$samples = @()
for ($iteration=0; $iteration -lt 3; $iteration++) {
    $raw = dotnet run --project $PSScriptRoot -c Debug --no-build -- probe $ArtifactDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Fresh-process probe failed.' }
    $samples += $raw | ConvertFrom-Json
}
$json = $samples | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $ArtifactDirectory 'process-cold.json'), $json)
$json
