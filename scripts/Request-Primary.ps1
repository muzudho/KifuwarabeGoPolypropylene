# Queue a primary-slot change for the next wrapper launch; never interrupt a running game.
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet(1,2,3)][int]$Primary,
    [string]$RootDirectory = 'D:\Projects\KifuwarabeGoPolypropylene',
    [string]$RequestId = ([guid]::NewGuid().ToString('N'))
)
$ErrorActionPreference = 'Stop'
$config = Get-Content -LiteralPath (Join-Path $RootDirectory 'engine-switch.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (@($config.engines).Count -ne 3) { throw 'Exactly three engine slots are required.' }
$enginePath = [IO.Path]::GetFullPath([string]$config.engines[$Primary-1].path, $RootDirectory)
$name = [IO.Path]::GetFileNameWithoutExtension($enginePath)
$directory = [IO.Path]::GetDirectoryName($enginePath)
foreach ($file in @($enginePath, (Join-Path $directory "$name.dll"), (Join-Path $directory "$name.deps.json"), (Join-Path $directory "$name.runtimeconfig.json"))) {
    if (!(Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing engine runtime file: $file" }
}
$destination = Join-Path $RootDirectory 'request-primary.json'
if ($PSCmdlet.ShouldProcess($destination, "Request slot $Primary for next launch")) {
    $temporary = Join-Path $RootDirectory ('.request-primary.' + [guid]::NewGuid().ToString('N') + '.tmp')
    try {
        $request = [ordered]@{ 'request-primary' = $Primary; 'request-id' = $RequestId }
        $json = [regex]::Replace(($request | ConvertTo-Json), '\r\n|\r|\n', "`r`n") + "`r`n"
        [IO.File]::WriteAllText($temporary, $json, [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporary, $destination, $true)
    }
    finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
    Write-Output "Queued slot $Primary; request-id=$RequestId. Applied at the next wrapper launch."
}
