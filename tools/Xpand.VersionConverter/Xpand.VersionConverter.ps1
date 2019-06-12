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
    [string]$projectFile ="C:\Users\Tolis\source\repos\Solution174\Solution174.Module\Solution174.Module.csproj",
    [string]$targetPath ="C:\Users\Tolis\source\repos\Solution174\Solution174.Module\bin\Debug\",
    [string]$DevExpressVersion,
    [string]$VerboseOutput,
    [string]$referenceFilter = "DevExpress*",
    [string]$assemblyFilter = "Xpand.XAF.*"
)

$howToVerbose="Edit $projectFile and enable verbose messaging by adding <PropertyGroup><VersionConverterVerbose>Continue</VersionConverterVerbose>. Rebuild the project and send the output to support."
if ($VerboseOutput){
    $VerbosePreference = $VerboseOutput
}

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\Functions.ps1"

Write-Verbose "Running VersionConverter on project $projectFile with target $targetPath"

$dxVersion = Get-DevExpressVersion $targetPath $referenceFilter $projectFile 
if (!$dxVersion){
    Write-Warning "Cannot find DevExpress Version. You have the following options:`r`n1. $howToVerbose`r`n2. If your project has indirect references to DevExpress through another assembly then you can always force the DevExpressVersion by modifying your project to include <PropertyGroup><DevExpressVersion>19.1.3</DevExpressVersion>.`r`n This declaration can be solution wide if done in your directory.build.props file.`r`n"
    throw "Check output warning message"
}
Write-Verbose "DxVersion=$dxVersion"

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
                        Write-Verbose "Checking $modulePath references..`r`n"
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
    Write-Warning ($_.Exception | Format-List -Force | Out-String) -ErrorAction Continue
    Write-Warning ($_.InvocationInfo | Format-List -Force | Out-String) -ErrorAction Continue
    Write-Warning "`r`n$howToVerbose`r`n"
    throw "Check output warning message"

}
finally {
    try {
        $mtx.ReleaseMutex() | Out-Null
        $mtx.Dispose() | Out-Null
    }
    catch {
        
    }
}
