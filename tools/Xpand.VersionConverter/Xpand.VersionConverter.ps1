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
    [string]$projectFile = "C:\Work\eXpandFramework\DevExpress.XAF\src\Tests\ALL\ALL.Win.Tests\ALL.Win.Tests.csproj",
    [string]$targetPath = "C:\Work\eXpandFramework\DevExpress.XAF\bin\AllTestWin\",
    $DevExpressVersion,
    [string]$VerboseOutput = "Continue",
    [string]$referenceFilter = "DevExpress*"
)

$howToVerbose = "Edit $projectFile and enable verbose messaging by adding <PropertyGroup><VersionConverterVerbose>Continue</VersionConverterVerbose>. Rebuild the project and send the output to support."
if ($VerboseOutput) {
    $VerbosePreference = $VerboseOutput
}
if ($DevExpressVersion) {
    [version]$DevExpressVersion = $DevExpressVersion
}
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\Functions.ps1"

Write-Verbose "Running VersionConverter on project $projectFile with target $targetPath"
if (!$DevExpressVersion) {
    $dxVersion = Get-DevExpressVersion $targetPath $referenceFilter $projectFile 
}
else {
    $dxVersion = $DevExpressVersion
}

if (!$dxVersion) {
    Write-Warning "Cannot find DevExpress Version. You have the following options:`r`n1. $howToVerbose`r`n2. If your project has indirect references to DevExpress through another assembly then you can always force the DevExpressVersion by modifying your project to include <PropertyGroup><DevExpressVersion>19.1.3</DevExpressVersion>.`r`n This declaration can be solution wide if done in your directory.build.props file.`r`n"
    throw "Check output warning message"
}
Write-Verbose "DxVersion=$dxVersion"

$packagesFolder = Get-PackagesFolder
Write-Verbose "nugetPackageFoldersPath=$packagesFolder"
$nugetPackageFolders = [Path]::GetFullPath($packagesFolder)
$moduleDirectories = [Directory]::GetDirectories($nugetPackageFolders) | Where-Object {
    $baseName = (Get-Item $_).BaseName
    $baseName -like "Xpand.XAF*" -or $baseName -like "Xpand.Extensions*"
}

$unpatchedPackages = Get-UnPatchedPackages $moduleDirectories $dxversion
if (!$unpatchedPackages) {
    Write-Verbose "All packages already patched for $dxversion"
    return
}
wh "ModuleDirectories:" -Section
$moduleDirectories
wh "UnpatchedPackages:" -Section
$unpatchedPackages

try {
    $mtx = [Mutex]::OpenExisting("VersionConverterMutex")
}
catch {
    $mtx = [Mutex]::new($false, "VersionConverterMutex")
}
$mtx.WaitOne() | Out-Null
try {    
    
    Install-MonoCecil $targetPath
    $assemblyList=Get-ChildItem $packagesFolder -Include "*.dll","*.exe" -Recurse|Where-Object{$_.GetType() -ne [DirectoryInfo]}
    $unpatchedPackages | Get-Item | ForEach-Object {
        $packageFile = $_.FullName
        $packageDir = $_.DirectoryName
        $versionConverterFlag = "$packageDir\VersionConverter.v.$dxVersion.DoNotDelete"
        if (!(Test-Path $versionConverterFlag)) {
            wh "Analyzing.. $packageFile" -section
            Remove-PatchFlags $packageDir 
            @($packageFile) | ForEach-Object {
                if (Test-Path $_) {
                    $modulePath = (Get-Item $_).FullName
                    wh "Switch:" -Style Underline
                    $a = @{
                        Modulepath      = $modulePath
                        Version         = $dxversion
                        referenceFilter = $referenceFilter
                        snkFile        = "$PSScriptRoot\Xpand.snk"
                        AssemblyList=$assemblyList
                    }
                    $a
                    Switch-ReferencesVersion @a
                }
            }
            "Flag $versionConverterFlag"
            New-Item $versionConverterFlag -ItemType Directory | Out-Null
        }
    }
}
catch {
    wh "Exception:" -ForegroundColor Red -Section
    $_
    wh "InvokcationInfo:" -ForegroundColor Yellow -Section
    $_.InvocationInfo
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
