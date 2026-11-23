param(
    [string]$SettingsRoot = 'C:\Users\muzud\AppData\Local\KifuwarabeGo2026',
    [string]$EngineDirectory = 'C:\MuzudhoApps\KifuwarabeGoPolypropyleneThinkEngine'
)
$ErrorActionPreference = 'Stop'
$exe = Join-Path $EngineDirectory 'KifuwarabeGoPolypropylene.ThinkEngine.exe'
if (!(Test-Path -LiteralPath $exe)) { throw "Engine not found: $exe" }
$engineFile = Join-Path $SettingsRoot 'GtpEngines\gtp-engine-list.json'
$playerFile = Join-Path $SettingsRoot 'Players\player-list.json'
$targetFile = Join-Path $SettingsRoot 'Targets\target-list.json'
$engines = Get-Content -LiteralPath $engineFile -Raw | ConvertFrom-Json
$players = Get-Content -LiteralPath $playerFile -Raw | ConvertFrom-Json
$targets = Get-Content -LiteralPath $targetFile -Raw | ConvertFrom-Json
$engine = @($engines.gtpEngines | Where-Object { $_.displayName -eq 'Kifuwarabe Go Polypropylene' })
if ($engine.Count -ne 1) { throw 'Expected exactly one existing Polypropylene engine profile.' }
$engine = $engine[0]
$engine.executablePath = $exe
$engine.workingDirectory = $EngineDirectory
$player = @($players.players | Where-Object { $_.engineProfileId -eq $engine.id })
if ($player.Count -eq 0) {
    $targetId = [guid]::NewGuid().ToString('N')
    $targets.targets = @($targets.targets) + [pscustomobject]@{
        id = $targetId; displayName = 'Client Identity'; connectionProfileId = ''
        loginName = ''; loginPass = ''; comment = ''
    }
    $players.players = @($players.players) + [pscustomobject]@{
        id = [guid]::NewGuid().ToString('N'); displayName = 'Kifuwarabe Go Polypropylene'
        identifier = 'Kifuwarabe Go Polypropylene'; kind = 'computer'
        engineProfileId = $engine.id; targetProfileIds = @($targetId)
    }
}
# 変更前のファイルをすべて退避してから保存する。再実行でプレイヤーを重複追加しない。
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-ffff'
foreach ($file in @($engineFile, $playerFile, $targetFile)) {
    Copy-Item -LiteralPath $file -Destination "$file.$stamp.bak"
}
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($engineFile, ($engines | ConvertTo-Json -Depth 30), $utf8)
[System.IO.File]::WriteAllText($targetFile, ($targets | ConvertTo-Json -Depth 30), $utf8)
[System.IO.File]::WriteAllText($playerFile, ($players | ConvertTo-Json -Depth 30), $utf8)
Write-Output 'Registered Kifuwarabe Go Polypropylene. Restart the lobby to reload settings.'
