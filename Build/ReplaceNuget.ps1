using namespace system.text.RegularExpressions
using namespace System.IO
param(
    $ProjectPath = "C:\Work\Reactive.XAF\src\Modules\Windows\Xpand.XAF.Modules.Windows.csproj",
    $TargetPath = "C:\Work\Reactive.XAF\bin\net461\Xpand.XAF.Modules.Windows.dll",
    $SkipNugetReplace,
    [switch]$FixVersion
)
function Update-AssemblyInfoVersion {
    param (
        [parameter(mandatory)]$version, 
        [parameter(ValueFromPipeline)][string]$path
    )
    
    begin {     
    }
    
    process {
        if (!$path) {
            $path = "."
        }
        Get-ChildItem -path $path -filter "*AssemblyInfo.cs" -Recurse|ForEach-Object {
            $c = Get-Content $_.FullName
            $result = $c -creplace 'Version\("([^"]*)', "Version(""$version"
            Set-Content $_.FullName $result
        }        
        Get-ChildItem -path $path -filter "*AssemblyInfoVersion.cs" -Recurse|ForEach-Object {
            $c = Get-Content $_.FullName
            $regex = [regex] '(?s)Version = "([^"]*)'
            $result = $regex.Replace($c, "Version = `"$version")
            Set-Content $_.FullName $result
        }        
    }
    
    end {
        
    }
}
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
                Version = [version]::Parse(([System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).fileversion))
                BaseName=$_.BaseName
            }
        }|Group-Object BaseName|ForEach-Object{
            $_.group|Where-Object{$_.item.FullName -notmatch "net461"}|ForEach-Object{
                $item=$_.item
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
}