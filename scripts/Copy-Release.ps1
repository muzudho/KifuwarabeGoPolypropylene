# Copies the four runtime files from an existing Release build.
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$SourceDirectory = 'C:\MuzudhoProjects\github.com\muzudho\KifuwarabeGoPolypropylene\KifuwarabeGoPolypropylene.ThinkEngine\bin\Release\net10.0'
)
$ErrorActionPreference = 'Stop'
$destinationDirectory = $PSScriptRoot
$baseName = 'KifuwarabeGoPolypropylene.ThinkEngine'
$files = @("$baseName.exe", "$baseName.dll", "$baseName.deps.json", "$baseName.runtimeconfig.json")

# Check every source before changing any destination files.
foreach ($name in $files) {
    $sourceFile = Join-Path $SourceDirectory $name
    if (!(Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
        throw "Release file not found: $sourceFile. Build in Release mode first."
    }
}
foreach ($name in $files) {
    $sourceFile = Join-Path $SourceDirectory $name
    $destinationFile = Join-Path $destinationDirectory $name
    if ($PSCmdlet.ShouldProcess($destinationFile, 'Copy Release file')) {
        Copy-Item -LiteralPath $sourceFile -Destination $destinationFile -Force
        Write-Host "Copied: $name"
    }
}
