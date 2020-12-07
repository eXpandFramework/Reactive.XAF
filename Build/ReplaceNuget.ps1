using namespace system.text.RegularExpressions
using namespace System.IO
param(
    $ProjectPath = "C:\Work\eXpandFramework\DevExpress.XAF\src\Modules\Reactive\Xpand.XAF.Modules.Reactive.csproj",
    $TargetPath = "C:\Work\eXpandFramework\DevExpress.XAF\bin\Xpand.XAF.Modules.Reactive.dll",
    $SkipNugetReplace,
    [switch]$FixVersion
)
$ErrorActionPreference = "Stop"
$VerbosePreference="continue"
$nugetFolder = "$env:USERPROFILE\.nuget\packages"    
if ((Test-Path $nugetFolder) -and !$SkipNugetReplace) {
    
    $targetItem=Get-Item $targetPath
    $packageFolder = Get-ChildItem $nugetFolder "$(((Get-Item $ProjectPath).BaseName))"
    if ($packageFolder){
        $projectItem=(Get-Item $ProjectPath)
        $assemblyName = $projectItem.BaseName
        Get-ChildItem $packageFolder.FullName "$assemblyName.dll" -Recurse | ForEach-Object {
            [PSCustomObject]@{
                Item = $_
                Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).fileversion
                BaseName=$_.BaseName
            }
        }|Group-Object BaseName|ForEach-Object{
            $item=($_.group|Sort-Object Version -Descending|Select-Object -First 1).Item
            if ($FixVersion){
                $destinationVersion=$item.Directory.Parent.Parent.Name
                Update-AssemblyInfoVersion $destinationVersion  $projectItem.DirectoryName
            }
            else{
                $destination=$item.FullName
                Copy-Item $TargetPath $destination -Force -Verbose
                Copy-Item "$($targetItem.DirectoryName)\$($targetItem.BaseName).pdb" $item.DirectoryName -Force -Verbose
            }
            
        }
    }
}