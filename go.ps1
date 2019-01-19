param(
    [string]$packageSources="C:\Program Files (x86)\DevExpress 18.2\Components\System\Components\packages",
    [string]$msbuild=$null,
    [string]$nugetApiKey=$null,
    [bool]$build=$true,
    [bool]$cleanBin=$true
)

Import-Module "$PSScriptRoot\tools\psake\psake.psm1" -Force 
& "$PSScriptRoot\tools\Build\ImportXpandPosh.ps1"

Invoke-psake  "$PSScriptRoot\Build.ps1" -properties @{
    "cleanBin"= $cleanBin;
    "msbuild"=$msbuild;
    "nugetApiKey"=$nugetApiKey;
    "packageSources"=$packageSources;
    "build"=$build;
}
