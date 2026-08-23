$ErrorActionPreference = "Stop"
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $dir

Write-Host "Building lean-ctx-launcher.exe (self-contained, single-file)..." -ForegroundColor Cyan

# Build launcher
Push-Location "$dir\Launcher"
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD FAILED" -ForegroundColor Red
    Pop-Location; Pop-Location
    exit 1
}
Pop-Location

$launcher = "$dir\Launcher\bin\Release\net10.0\win-x64\publish\lean-ctx-launcher.exe"
if (-not (Test-Path $launcher)) {
    Write-Host "ERROR: Launcher exe not found" -ForegroundColor Red
    Pop-Location; exit 1
}

Write-Host "✓ Built: $launcher" -ForegroundColor Green
Pop-Location
