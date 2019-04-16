param(
    [string]$packageSources = "C:\Program Files (x86)\DevExpress 18.2\Components\System\Components\packages",
    [string]$msbuild = $null,
    [string]$nugetApiKey = $null,
    [string]$dxVersion = "18.2.7",
    [bool]$build = $true,
    [bool]$cleanBin = $true,
    [string]$branch="master",
    [switch]$InstallModules
)
$ErrorActionPreference = "Stop"
@([PSCustomObject]@{
    Name = "psake"
    Version ="4.7.4"
},[PSCustomObject]@{
    Name = "XpandPosh"
    Version ="1.8.1"
})|ForEach-Object{
    & "$PSScriptRoot\tools\build\Install-Module.ps1" $_
}
if ($InstallModules){
    return
}
Invoke-XPsake  "$PSScriptRoot\Build.ps1" -properties @{
    "cleanBin"       = $cleanBin;
    "msbuild"        = $msbuild;
    "nugetApiKey"    = $nugetApiKey;
    "packageSources" = $packageSources;
    "build"          = $build;
    "dxVersion"          = $dxVersion;
    "branch"=$branch;
}
