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

set-location $targetPath
Write-Verbose "Running Version Converter on project $projectFile with target $targetPath"
$projectFileInfo = Get-Item $projectFile
[xml]$csproj = Get-Content $projectFileInfo.FullName
$references = $csproj.Project.ItemGroup.Reference
$dxReferences = $references | Where-Object { $_.Include -like "$referenceFilter" }    
$dxVersion = Get-DevExpressVersion $targetPath $referenceFilter $dxReferences | Where-Object { $_ } | Select-Object -First 1
$analyze = $references | Where-Object { 
    if ($_.Include -like "$assemblyFilter") {
        $packageFile = "$($projectFileInfo.DirectoryName)\$($_.HintPath)"
        if (Test-Path $packageFile) {
            $packageDir = (Get-Item $packageFile).DirectoryName
            $exists = (Test-Path "$packageDir\VersionConverter.v.$dxVersion.DoNotDelete")
            !$exists
        }
    }
} | Select-Object -First 1
if (!$analyze) {
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
    Write-Verbose "Loading Mono.Cecil"
    $monoPath = "$PSScriptRoot\mono.cecil.0.10.3\lib\net40"
    if (!(Test-Path "$monoPath\Mono.Cecil.dll")) {
        $client = New-Object System.Net.WebClient
        $client.DownloadFile("https://www.nuget.org/api/v2/package/Mono.Cecil/0.10.3", "$PSScriptRoot\mono.cecil.0.10.3.zip")
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [ZipFile]::ExtractToDirectory("$PSScriptRoot\mono.cecil.0.10.3.zip", "$PSScriptRoot\mono.cecil.0.10.3")
    }

    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.dll")) | Out-Null
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.pdb.dll")) | Out-Null
    Add-Type @"
using Mono.Cecil;
public class MyDefaultAssemblyResolver : DefaultAssemblyResolver{
    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters){
        try{
            return base.Resolve(name, parameters);
        }
        catch (AssemblyResolutionException){
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(string.Format(@"$targetPath\{0}.dll", name.Name));
            return assemblyDefinition;
        }
    }
}
"@ -ReferencedAssemblies @("$monoPath\Mono.Cecil.dll")
    $references | Where-Object { $_.Include -like $assemblyFilter } | ForEach-Object {
        $packageFile = "$($projectFileInfo.DirectoryName)\$($_.HintPath)"
        $packageDir = (Get-Item $packageFile).DirectoryName
        Get-ChildItem $packageDir *VersionConverter.v.* -Exclude $dxVersion | ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force
        }
        $versionConverterFlag = "$packageDir\VersionConverter.v.$dxVersion.DoNotDelete"
        if (!(Test-Path $versionConverterFlag)) {
            "$targetPath\$([Path]::GetFileName($_.HintPath))", $packageFile | ForEach-Object {
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
