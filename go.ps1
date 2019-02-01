param(
    [string]$packageSources = "C:\Program Files (x86)\DevExpress 18.2\Components\System\Components\packages",
    [string]$msbuild = $null,
    [string]$nugetApiKey = $null,
    [string]$dxVersion = "18.2.5",
    [bool]$build = $true,
    [bool]$cleanBin = $true,
    [string]$branch="master"
)
$ErrorActionPreference = "Stop"
& "$PSScriptRoot\tools\build\Install-Module.ps1" $([PSCustomObject]@{
    Name = "psake"
    Version ="4.7.4"
})
Invoke-XPsake  "$PSScriptRoot\Build.ps1" -properties @{
    "cleanBin"       = $cleanBin;
    "msbuild"        = $msbuild;
    "nugetApiKey"    = $nugetApiKey;
    "packageSources" = $packageSources;
    "build"          = $build;
    "dxVersion"          = $dxVersion;
    "branch"=$branch;
}
