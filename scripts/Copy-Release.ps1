# Copy one complete engine runtime into an explicitly selected slot.
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet(1, 2, 3)][int]$Slot,
    [string]$SourceDirectory = 'D:\github.com\muzudho\KifuwarabeGoPolypropylene\KifuwarabeGoPolypropylene.ThinkEngine\bin\Release\net10.0',
    [string]$DestinationRoot = 'D:\Projects\KifuwarabeGoPolypropylene'
)
$ErrorActionPreference = 'Stop'
$destinationDirectory = Join-Path $DestinationRoot "${Slot}系"
$baseName = 'KifuwarabeGoPolypropylene.ThinkEngine'
$files = @("$baseName.exe", "$baseName.dll", "$baseName.deps.json", "$baseName.runtimeconfig.json")
# Validate the entire source set before touching the destination.
foreach ($name in $files) {
    $sourceFile = Join-Path $SourceDirectory $name
    if (!(Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
        throw "Release file not found: $sourceFile. Build in Release mode first."
    }
}
if ([IO.Path]::GetFullPath($SourceDirectory).TrimEnd('\') -eq [IO.Path]::GetFullPath($destinationDirectory).TrimEnd('\')) {
    throw 'Source and destination must be different directories.'
}
if ($PSCmdlet.ShouldProcess($destinationDirectory, 'Copy all four engine runtime files')) {
    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    foreach ($name in $files) {
        Copy-Item -LiteralPath (Join-Path $SourceDirectory $name) -Destination (Join-Path $destinationDirectory $name) -Force
    }
    Write-Host "Copied engine runtime to: $destinationDirectory"
}
