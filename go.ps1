param(
    [string]$version="18.2.300.1",
    [string]$packageSources="https://nuget.devexpress.com/88luCgoeuPFTrDrTDcc6zKg22U2cVcTm3vdKCv88I7PHF9St6i/api",
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
    "packageSources"=$packageSources;
    "build"=$build
}
