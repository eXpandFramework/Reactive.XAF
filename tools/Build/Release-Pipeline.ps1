param(
    $Branch,
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName,
    $Pass,
    $DXApiFeed,
    $artifactstagingdirectory
)
$ErrorActionPreference = "Stop"
& "$SourcePath\go.ps1" -InstallModules 

$bArgs=@{
    packageSources="$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist="release"
    Release=$true
}
& $SourcePath\go.ps1 @bArgs

"$SourcePath\Bin\Nupkg","$SourcePath\Bin\Nuspec"|ForEach-Object{
    Get-ChildItem $_ -Recurse |ForEach-Object{
        Copy-Item $_.FullName -Destination $artifactstagingdirectory
    }
}