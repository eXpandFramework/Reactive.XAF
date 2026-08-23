$ErrorActionPreference = "Stop"

$nodeDir = "C:\nvm4w\nodejs"
$ps1Bak = "$nodeDir\lean-ctx.ps1.bak"
$leanCtxLauncher = "$nodeDir\lean-ctx-launcher.exe"

if (Test-Path $ps1Bak) {
    Copy-Item $ps1Bak "$nodeDir\lean-ctx.ps1" -Force
    Remove-Item $ps1Bak
    Write-Host "  Restored: lean-ctx.ps1 from backup" -ForegroundColor Gray
}

if (Test-Path $leanCtxLauncher) {
    Remove-Item $leanCtxLauncher -Force
    Write-Host "  Removed: lean-ctx-launcher.exe" -ForegroundColor Gray
}

Write-Host "✓ Uninstalled!" -ForegroundColor Green
