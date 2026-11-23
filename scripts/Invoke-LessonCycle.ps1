#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateRange(1, 3600)][int]$TimeoutSeconds = 300,
    [ValidateRange(1, 10000)][int]$Games = 32,
    [int]$Seed = 1,
    [string]$ArtifactRoot = ('Z:\muzudho-with-ai-agent\KifuwarabeGoPolypropylene\tmp\delete-after-' + (Get-Date).AddDays(7).ToString('yyyy-MM-dd') + '\lesson-cycle')
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
if (!(Test-Path -LiteralPath (Split-Path $ArtifactRoot -Qualifier))) {
    throw "Artifact drive unavailable: $ArtifactRoot. No local fallback is generated."
}
New-Item -ItemType Directory -Force -Path $ArtifactRoot | Out-Null
# 同時実行を防ぐ。残った空ファイルではなく、OS の排他ロックで判定する。
$lockFile = Join-Path $ArtifactRoot 'cycle.lock'
try { $cycleLock = [System.IO.File]::Open($lockFile, 'OpenOrCreate', 'ReadWrite', 'None') }
catch { throw "Another cycle may be running, or NAS is unavailable: $($_.Exception.Message)" }
$runDirectory = Join-Path $ArtifactRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-ffff') + "-seed$Seed")
$watch = [System.Diagnostics.Stopwatch]::StartNew()
$status = 'failed'
$detail = ''
function Invoke-BoundedProcess([string]$Label, [string]$FileName, [string[]]$Arguments) {
    $remaining = $TimeoutSeconds * 1000 - [int]$watch.ElapsedMilliseconds
    if ($remaining -le 0) { throw [System.TimeoutException]::new('Cycle time budget exhausted') }
    $start = [System.Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $FileName
    $start.WorkingDirectory = $repository
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $start
    try {
        $null = $process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $exited = $process.WaitForExit($remaining)
        if (!$exited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        [System.IO.File]::WriteAllText((Join-Path $runDirectory "$Label.stdout.log"), $stdout.GetAwaiter().GetResult())
        [System.IO.File]::WriteAllText((Join-Path $runDirectory "$Label.stderr.log"), $stderr.GetAwaiter().GetResult())
        if (!$exited) { throw [System.TimeoutException]::new("$Label exceeded the cycle budget; process tree terminated") }
        if ($process.ExitCode -eq 2 -and $Label -eq 'cycle') { throw [System.TimeoutException]::new('Engine reached its cooperative time limit') }
        if ($process.ExitCode -ne 0) { throw "$Label failed with exit code $($process.ExitCode)" }
    }
    finally { $process.Dispose() }
}
try {
    New-Item -ItemType Directory -Path $runDirectory | Out-Null
    $sourceFiles = @(Get-ChildItem (Join-Path $repository 'KifuwarabeGoPolypropylene.ThinkEngine') -File | Where-Object { $_.Extension -in @('.cs', '.csproj') })
    $sourceFiles | Get-FileHash -Algorithm SHA256 | Select-Object Path, Hash | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $runDirectory 'source-hashes.json')
    $buildDirectory = Join-Path $runDirectory 'build'
    Invoke-BoundedProcess 'build' 'dotnet' @('build', (Join-Path $repository 'KifuwarabeGoPolypropylene.slnx'), '-c', 'Release', '--artifacts-path', $buildDirectory)
    $assembly = @(Get-ChildItem (Join-Path $buildDirectory 'bin') -Recurse -File -Filter 'KifuwarabeGoPolypropylene.ThinkEngine.dll')
    if ($assembly.Count -ne 1) { throw 'Expected exactly one built engine assembly' }
    Invoke-BoundedProcess 'tests' 'dotnet' @($assembly[0].FullName, '--self-test')
    $secondsLeft = [Math]::Max(1, [int][Math]::Floor($TimeoutSeconds - $watch.Elapsed.TotalSeconds - 1))
    Invoke-BoundedProcess 'cycle' 'dotnet' @($assembly[0].FullName, '--lesson-cycle', '--seed', "$Seed", '--games', "$Games", '--time-limit', "$secondsLeft", '--output', $runDirectory)
    $status = 'completed'
}
catch [System.TimeoutException] { $status = 'timeout'; $detail = $_.Exception.Message }
catch { $detail = $_.Exception.Message }
finally {
    try {
        if (Test-Path -LiteralPath $runDirectory) {
            @{ status = $status; detail = $detail; timeoutSeconds = $TimeoutSeconds; elapsedSeconds = $watch.Elapsed.TotalSeconds; seed = $Seed; games = $Games; output = $runDirectory } |
                ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $runDirectory 'run-status.json')
        }
    }
    finally { $cycleLock.Dispose() }
}
Write-Output "Cycle $status : $runDirectory"
if ($detail) { Write-Output $detail }
if ($status -eq 'timeout') { exit 2 }
if ($status -ne 'completed') { exit 1 }
