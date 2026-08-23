$ErrorActionPreference = "Stop"

$nodeDir = "C:\nvm4w\nodejs"
$launcherSrc = "$PSScriptRoot\Launcher\bin\Release\net10.0\win-x64\publish\lean-ctx-launcher.exe"
$ps1Orig = "$nodeDir\lean-ctx.ps1"
$ps1Bak = "$nodeDir\lean-ctx.ps1.bak"
$leanCtxLauncher = "$nodeDir\lean-ctx-launcher.exe"

if (-not (Test-Path $launcherSrc)) {
    Write-Host "ERROR: Launcher not built. Run build.ps1 first." -ForegroundColor Red
    exit 1
}

# Backup original .ps1 if not already done
if (Test-Path $ps1Orig) {
    if (-not (Test-Path $ps1Bak)) {
        Copy-Item $ps1Orig $ps1Bak
        Write-Host "  Backed up: lean-ctx.ps1 -> lean-ctx.ps1.bak" -ForegroundColor Gray
    }
}

# Copy launcher to nodejs dir
Copy-Item $launcherSrc $leanCtxLauncher -Force
Write-Host "  Copied launcher: $leanCtxLauncher" -ForegroundColor Gray

# Replace lean-ctx.ps1 with a wrapper that calls our launcher
@"
#!/usr/bin/env pwsh
`$basedir = Split-Path `$MyInvocation.MyCommand.Definition -Parent
& "`$basedir\lean-ctx-launcher.exe" @args
exit `$LASTEXITCODE
"@ | Set-Content $ps1Orig -Encoding ASCII

Write-Host "  Replaced: lean-ctx.ps1 -> launcher wrapper" -ForegroundColor Gray

# Clean up old DLL-based artifacts
Remove-Item "$nodeDir\NoFlashProxy.dll" -ErrorAction SilentlyContinue
Remove-Item "$nodeDir\version.dll" -ErrorAction SilentlyContinue

Write-Host "✓ Installed! lean-ctx now hooks CreateProcessW with shellcode" -ForegroundColor Green
Write-Host "  No DLL needed — pure inline hook" -ForegroundColor Gray
Write-Host "  Restore with: uninstall.ps1" -ForegroundColor Gray
