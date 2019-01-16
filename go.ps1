param(
    [string]$version=$null,
    [string]$packageSources="C:\Program Files (x86)\DevExpress 18.2\Components\System\Components\packages",
    [string]$msbuild=$null,
    [string]$nugetApiKey=$null,
    [bool]$build=$true,
    [bool]$cleanBin=$true
)

Import-Module "$PSScriptRoot\tools\psake\psake.psm1" -Force 
Import-Module "$PSScriptRoot\tools\XpandPsUtils\XpandPsUtils.psm1" -Force 

Invoke-psake  "$PSScriptRoot\Build.ps1" -properties @{
    "version"=$version;
    "cleanBin"= $cleanBin;
    "msbuild"=$msbuild;
    "nugetApiKey"=$nugetApiKey;
    "packageSources"=$packageSources;
    "build"=$build;
}
