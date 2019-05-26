using namespace System
using namespace System.Threading
using namespace System.Reflection
using namespace System.IO
using namespace System.IO.Compression
using namespace System.Reflection
using namespace System.Text.RegularExpressions
using namespace Mono.Cecil
using namespace Mono.Cecil.pdb
param(
    [string]$projectFile ,
    [string]$targetPath ,
    [string]$referenceFilter = "DevExpress*",
    [string]$assemblyFilter = "Xpand.XAF.*"
)
# $VerbosePreference = "Continue"
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\Functions.ps1"

Write-Verbose "Running Version Converter on project $projectFile with target $targetPath"

$dxVersion = Get-DevExpressVersion $targetPath $referenceFilter $projectFile 
$nugetPackageFoldersPath="$PSSCriptRoot\..\..\.."
if ((Get-Item "$PSScriptRoot\..").BaseName -like "Xpand.VersionConverter*"){
    $nugetPackageFoldersPath="$PSSCriptRoot\..\.."
}
Write-Verbose "nugetPackageFoldersPath=$nugetPackageFoldersPath"
$nugetPackageFolders=[Path]::GetFullPath($nugetPackageFoldersPath)
$moduleDirectories=[Directory]::GetDirectories($nugetPackageFolders)|Where-Object{(Get-Item $_).BaseName -like "Xpand.XAF*"}
if (!($moduleDirectories|where-Object{!(Get-ChildItem $_ "VersionConverter.v.$dxVersion.DoNotDelete" -Recurse|Select-Object -First 1)})){
    Write-Verbose "All packages already patched for $dxversion"
    return
}

try {
    $mtx = [Mutex]::OpenExisting("VersionConverterMutex")
}
catch {
    $mtx = [Mutex]::new($false, "VersionConverterMutex")
}
$mtx.WaitOne() | Out-Null
try {    
    Install-MonoCecil $targetPath
    $moduleDirectories|ForEach-Object{
        write-verbose "moduleDir=$_"
        Get-ChildItem $_ Xpand.XAF*.dll -Recurse|ForEach-Object{
            $packageFile = $_.FullName
            Write-verbose "packageFile=$packageFile"
            $packageDir = $_.DirectoryName
            Remove-OtherVersionFlags $packageDir $dxVersion
            $versionConverterFlag = "$packageDir\VersionConverter.v.$dxVersion.DoNotDelete"
            if (!(Test-Path $versionConverterFlag)) {
                "$targetPath\$([Path]::GetFileName($packageFile))", $packageFile | ForEach-Object {
                    if (Test-Path $_) {
                        $modulePath = (Get-Item $_).FullName
                        Write-Verbose "Checking $modulePath references.."
                        Update-Version $modulePath $dxVersion
                    }
                }
                Write-Verbose "Flag $versionConverterFlag"
                New-Item $versionConverterFlag -ItemType Directory | Out-Null
            }
        }
    }
}
catch {
     
    Write-Error ($_.Exception | Format-List -Force | Out-String) -ErrorAction Continue
    Write-Error ($_.InvocationInfo | Format-List -Force | Out-String) -ErrorAction Continue
    exit 1

}
finally {
    try {
        $mtx.ReleaseMutex() | Out-Null
        $mtx.Dispose() | Out-Null
    }
    catch {
        
    }
}
