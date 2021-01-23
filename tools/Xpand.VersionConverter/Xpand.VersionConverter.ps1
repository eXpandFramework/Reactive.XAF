using namespace System
using namespace System.Threading
using namespace System.Reflection
using namespace System.IO
using namespace System.IO.Compression
using namespace System.Text.RegularExpressions
using namespace Mono.Cecil
using namespace Mono.Cecil.pdb
param(
    [string]$projectFile ="C:\Work\eXpandFramework\DevExpress.XAF\src\Tests\EasyTests\TestApplication\TestApplication.Module\TestApplication.Module.csproj",
    [string]$MSBuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\",
    $DevExpressVersion,
    [string]$VerboseOutput = "Continue",
    [string]$referenceFilter,
    [string]$targetFilter ="(?is)Xpand\.XAF|Xpand\.Extensions"
)

if (!$referenceFilter){
    $referenceFilter="DevExpress*"
}
if (!$targetFilter){
    $targetFilter="(?is)Xpand\.XAF|Xpand\.Extensions"
}

$ErrorActionPreference = "Stop"
. "$PSScriptRoot\Functions.ps1"
$VerbosePreference=ConfigureVerbose $VerboseOutput VerboseVersionConverter
Write-VerboseLog "Executing VersionConverter..." 
Write-VerboseLog "ProjectFile=$projectFile"
Write-VerboseLog "targetPath=$targetPath"
Write-VerboseLog "DevExpressVersion=$DevExpressVersion"
Write-VerboseLog "referenceFilter=$referenceFilter"
Write-VerboseLog "targetFilter=$targetFilter"



$packagesFolder = Get-PackagesFolder
Write-VerboseLog "nugetPackageFoldersPath=$packagesFolder"
$referenceAssemblies=Resolve-Refs
$dxversion=($referenceAssemblies|Where-Object{$_.basename -match $referenceFilter}|Select-Object -First 1).Directory.Parent.Parent.Name
Write-VerboseLog "DxVersion=$dxVersion"

$unpatchedPackages = Get-UnPatchedPackages $referenceAssemblies $dxversion $targetFilter
if (!$unpatchedPackages) {
    Write-VerboseLog "All packages already patched for $dxversion"
    return
}

wh "UnpatchedPackages:" -Section
$unpatchedPackages.FullName

try {
    $mtx = [Mutex]::OpenExisting("VersionConverterMutex")
}
catch {
    $mtx = [Mutex]::new($false, "VersionConverterMutex")
}
$mtx.WaitOne() | Out-Null
try {    
    Install-MonoCecil $targetPath
    $r=[Mono.Cecil.DefaultAssemblyResolver]::new()
    $referenceAssemblies|ForEach-Object{
        $r.AddSearchDirectory($_.DirectoryName)  
    }
    $unpatchedPackages | Get-Item | ForEach-Object {
        $packageFile = $_.FullName
        $packageDir = $_.DirectoryName
        
        wh "Analyzing.. $packageFile" -section
        wh "Switch:" -Style Underline
        $a = @{
            Modulepath      = $packageFile
            Version         = $dxversion
            referenceFilter = $referenceFilter
            snkFile        = "$PSScriptRoot\Xpand.snk"
        }
        $a
        Switch-DependencyVersion @a
        "Flag $versionConverterFlag"
        [xml]$nuspec = Get-Content "$packageDir\..\..\$($_.BaseName).nuspec"
        $tags=$nuspec.package.metadata.tags.split(",")|Select-Object -SkipLast 1
        $tags+=$dxversion
        $nuspec.package.metadata.tags=$tags -join ", "
        $nuspec.Save("$packageDir\..\..\$($_.BaseName).nuspec")
    }
    $r.Dispose()
}
catch {
    throw "$_`r`n$($_.ScriptStackTrace)"
}
finally {
    try {
        $mtx.ReleaseMutex() | Out-Null
        $mtx.Dispose() | Out-Null
    }
    catch {
        
    }
}
