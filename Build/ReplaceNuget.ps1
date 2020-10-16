using namespace system.text.RegularExpressions
param(
    $ProjectPath = "C:\Work\eXpandFramework\DevExpress.XAF\src\Modules\CloneMemberValue\Xpand.XAF.Modules.CloneMemberValue.csproj",
    $TargetPath = "C:\Work\eXpandFramework\DevExpress.XAF\bin\Xpand.XAF.Modules.CloneMemberValue.dll",
    $SkipNugetReplace
)

$ErrorActionPreference = "Stop"
$nugetFolder = "$env:USERPROFILE\.nuget\packages"    
if ((Test-Path $nugetFolder) -and !$SkipNugetReplace) {
    $packageFolder = Get-ChildItem $nugetFolder "$(((Get-Item $ProjectPath).BaseName))"
    $assemblyName = (Get-Item $ProjectPath).BaseName
    Get-ChildItem $packageFolder.FullName "$assemblyName.dll" -Recurse | ForEach-Object {
        Copy-Item $TargetPath $_.FullName -Force -Verbose
    }
}