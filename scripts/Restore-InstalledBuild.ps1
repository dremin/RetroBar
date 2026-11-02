param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath
)
$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path -LiteralPath $BackupPath).Path
$installPath = Join-Path $env:LOCALAPPDATA 'Programs\RetroBar'
if (!(Test-Path -LiteralPath (Join-Path $sourcePath 'RetroBar.exe')) -or
    !(Test-Path -LiteralPath (Join-Path $sourcePath 'RetroBar.dll'))) {
    throw 'The backup must contain RetroBar.exe and RetroBar.dll.'
}
Get-Process RetroBar -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq (Join-Path $installPath 'RetroBar.exe') } |
    Stop-Process
Get-ChildItem -LiteralPath $sourcePath |
    Where-Object { $_.Name -ne 'settings.snapshot.json' } |
    Copy-Item -Destination $installPath -Recurse -Force
if ((Get-FileHash -LiteralPath (Join-Path $sourcePath 'RetroBar.dll')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $installPath 'RetroBar.dll')).Hash) {
    throw 'Restored DLL does not match backup.'
}
Start-Process -FilePath (Join-Path $installPath 'RetroBar.exe') -WindowStyle Hidden
Write-Output "Restored RetroBar from $sourcePath"
