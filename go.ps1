param(
    
    [string]$packageSources = $env:DxFeed, #"C:\Program Files (x86)\DevExpress 18.2\Components\System\Components\packages",
    [string]$msbuild = $null,
    [string]$nugetApiKey = $null,
    [string]$dxVersion = "18.2.11",
    [bool]$build = $true,
    [bool]$cleanBin = $true,
    [string]$branch="lab",
    [switch]$InstallModules,
    [string[]]$taskList=@("Build"),
    [string]$XpandPwshVersion = "1.201.34.2",
    [string[]]$ChangedModules=@(),
    [switch]$CustomVersion
)
$ErrorActionPreference = "Stop"

@([PSCustomObject]@{
    Name = "psake"
    Version ="4.7.4"
},[PSCustomObject]@{
    Name = "XpandPwsh"
    Version =$XpandPwshVersion
})|ForEach-Object{
    & "$PSScriptRoot\build\Install-Module.ps1" $_
}
"XpandPwshVersion=$((Get-Module XpandPwsh -ListAvailable).Version)"
if ($InstallModules){
    return
}

Invoke-XPsake  "$PSScriptRoot\build\BuildDevExpress.XAF.ps1" -properties @{
    "cleanBin"       = $cleanBin;
    "msbuild"        = $msbuild;
    "nugetApiKey"    = $nugetApiKey;
    "packageSources" = $packageSources;
    "build"          = $build;
    "dxVersion"          = $dxVersion;
    "branch"=$branch;
    "ChangedModules"=$ChangedModules;
    "CustomVersion"=$CustomVersion;
    "Root"=$PSScriptRoot;
} -taskList $taskList
