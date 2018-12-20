param(
    [string]$version="18.2.300.1",
    [string]$DXNugetApiFeed=$null,
    [string[]] $filter=@("*.nuspec"),# $filter=@("*SystemEx*.nuspec","Numeric*.nuspec"),
    [Parameter(ValueFromPipeline = $true)]
    [object[]]$nuspecFiles="\bin\nuspec\",
    [string]$msbuild=$null,
    [string]$nugetApiKey=$null,
    [bool]$build=$true,
    [bool]$cleanBin=$true
)
Import-Module "$PSScriptRoot\tools\psake\psake.psm1" -Force 
Import-Module "$PSScriptRoot\tools\XpandPsUtils\XpandPsUtils.psm1" -Force 

Invoke-psake  "$PSScriptRoot\Build.ps1" -properties @{
    "nuspecFiles"=$nuspecFiles;
    "version"=$version;
    "cleanBin"= $cleanBin;
    "msbuild"=$msbuild;
    "nugetApiKey"=$nugetApiKey;
    "filter"=$filter;
    "additionalSources"=$additionalSources;
    "DXNugetApiFeed"=$DXNugetApiFeed;
    "build"=$build
}
