[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Root=([System.IO.Path]::GetFullPath("$PSScriptRoot\..\"))
)
Write-HostFormatted "Building Xpand.XAF.ModelEditor.WinDesktop" -Section
Push-Location "$Root\Tools\Xpand.XAF.ModelEditor\"
dotnet publish ".\Xpand.XAF.ModelEditor.WinDesktop.csproj"
Set-Location ".\bin\Debug\net5.0-windows7.0\publish"
$zip=[System.IO.Path]::GetFullPath("$(Get-Location)\..\Xpand.XAF.ModelEditor.WinDesktop.zip")
Compress-Files -zipfileName $zip -Force
New-Item "$root\bin\zip" -ItemType Directory -Verbose
Copy-Item $zip "$root\bin\zip\Xpand.XAF.ModelEditor.WinDesktop.zip" -Verbose

if (!(Test-AzDevops)){
    Write-HostFormatted "Building Xpand.XAF.ModelEditor.Win" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\ModelEditor.Win\Xpand.XAF.ModelEditor.Win"
    dotnet publish "Xpand.XAF.ModelEditor.Win.csproj"
    Set-Location "$(Get-Location)\bin\Release\net5.0-windows\publish"
    $zip="$(Get-Location)\..\Xpand.XAF.ModelEditor.Win.zip"
    Compress-Files -zipfileName $zip -Force 
    $version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo("$(Get-Location)\Xpand.XAF.ModelEditor.Win.exe").FileVersion
    Move-Item $zip "$([System.IO.Path]::GetDirectoryName($zip))\$([System.IO.Path]::GetFileNameWithoutExtension($zip)).$version.zip" -Force
}

Pop-Location