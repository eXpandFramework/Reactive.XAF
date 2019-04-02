param(
    $Branch,
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName,
    $Pass,
    $DXApiFeed
)
$ErrorActionPreference = "Stop"
& "$SourcePath\go.ps1" -InstallModules

$bArgs=@{
    msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
    packageSources="$(Get-PackageFeed -Xpand);$DxApiFeed"
}
& $SourcePath\go.ps1 @bArgs


