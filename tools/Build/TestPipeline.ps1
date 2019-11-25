using namespace Mono.Cecil
using namespace System.Reflection
using namespace System.IO
param(
    $SourcePath = [Path]::GetFullPath("$PSScriptRoot\..\..")
)
$ErrorActionPreference="Stop"

$buildNumber=$env:build_BuildNumber
$buildNumber+=$env:Build_TriggeredBy_DefinitionName
. "$Sourcepath\Tools\Build\WriteHostFormatted.ps1"
Write-Verbose -Verbose "##vso[build.updatebuildnumber]$buildNumber"
$ErrorActionPreference = "Stop"
function Install-MonoCecilCustom($monopath) {
    Write-Verbose "Loading Mono.Cecil"
    [Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.dll")) | Out-Null
    [Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.pdb.dll")) | Out-Null
    
    Add-Type @"
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Mono.Cecil;

    public class MyDefaultAssemblyResolver : DefaultAssemblyResolver{
        List<AssemblyDefinition> _resolvedDefinitions=new List<AssemblyDefinition>();
        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters){
            var definition = ResolveAssemblyDefinition(name, parameters);
            _resolvedDefinitions.Add(definition);
            return definition;
        }

        private AssemblyDefinition ResolveAssemblyDefinition(AssemblyNameReference name, ReaderParameters parameters){
            try{
                return base.Resolve(name, parameters);
            }
            catch (AssemblyResolutionException){
                return AssemblyDefinition(name);
            }
        }

        protected override void Dispose(bool disposing){
            base.Dispose(disposing);
            foreach (var resolvedDefinition in _resolvedDefinitions){
                resolvedDefinition.Dispose();
            }
        }

        private static AssemblyDefinition AssemblyDefinition(AssemblyNameReference name){
            return Mono.Cecil.AssemblyDefinition.ReadAssembly(string.Format(@"$monoPath\{0}.dll", name.Name));
        }
    }
"@ -ReferencedAssemblies @("$monoPath\Mono.Cecil.dll")
}
function UpdateVersion {
    param (
        $_,
        $monoPath
    )
    if (Test-Path "$monoPath\$($_.Name)") {
        Write-HostFormatted "updating $monoPath\$($_.Name)" -Section
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$monoPath\$($_.Name)").FileVersion
        $assemblyResolver = New-Object MyDefaultAssemblyResolver
        $readerParams = New-Object ReaderParameters
        $readerParams.AssemblyResolver = $assemblyResolver
        $readerParams.ReadWrite = $true
        $moduleDefinition = [ModuleDefinition]::ReadModule($_.FullName, $readerParams)
        $attribute = $moduleDefinition.Assembly.CustomAttributes | Where-Object { $_.AttributeType.Name -eq "AssemblyFileVersionAttribute" }
        $attribute.ConstructorArguments.RemoveAt(0)
        $newArg = New-Object Mono.Cecil.CustomAttributeArgument($moduleDefinition.TypeSystem.String, "$version")
        $attribute.ConstructorArguments.Add($newArg)
        $currentVersion=$moduleDefinition.Assembly.Name.Version
        $moduleDefinition.Assembly.Name.Version = $version    
        if ($currentVersion -ne $version){
            $moduleDefinition.Write() 
            Write-HostFormatted "$($_.BaseName) version changed from $currentVersion to $version" -ForegroundColor Purple
        }
        
        $moduleDefinition.Dispose()
    }
}
$xpandBlobOwnerSecret = "7T._y-49CewMhZuFLNAf0-@h1L8kwML_"
$azureAplicationId = "0de13596-c3d2-4dcf-8248-bfeaec927a76"

$subscriptionTenantId = "e4d60396-9352-4ae8-b84c-e69244584fa4"
$azurePassword = ConvertTo-SecureString $xpandBlobOwnerSecret -AsPlainText -Force
$psCred = New-Object System.Management.Automation.PSCredential($azureAplicationId , $azurePassword)
Connect-AzAccount -Credential $psCred -TenantId $subscriptionTenantId  -ServicePrincipal 
$updates = [System.IO.Path]::GetFullPath("$SourcePath\bin\updates")
Remove-Item $updates -Recurse -Force -ErrorAction SilentlyContinue
New-Item $updates -ItemType Directory -Force

Write-HostFormatted  "Downloading into $updates" -Section

Get-AzStorageBlob -Container xpandbuild -Context (Get-AzStorageAccount).Context | Get-AzStorageBlobContent -Destination $updates -Force|Out-Null
Write-HostFormatted "Download Finished" -ForegroundColor Green
$monoPath = "$SourcePath\bin\"
"Installing Mono.Cecil"
Install-MonoCecilCustom $monopath
. $SourcePath\tools\Xpand.VersionConverter\functions.ps1
[version]$dxVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(((Get-ChildItem $monoPath *DevExpress*.dll).FullName | Select-Object -First 1)).FileVersion
Write-HostFormatted "dxVersion=$dxVersion" -ForegroundColor Green
$snk="$Sourcepath\src\Xpand.key\Xpand.snk"
Get-ChildItem $updates *.dll | ForEach-Object {
    Write-HostFormatted "DevExpress Version Patching $($_.BaseName) $($dxVersion)" -Section
    Update-Version $_.FullName $dxVersion DevExpress* $snk
    UpdateVersion $_ $monoPath
}
Get-ChildItem $monoPath "*Xpand.*.dll"|ForEach-Object{
    $version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion
    $fileName=$_.BaseName
    Get-ChildItem $updates *.dll |Where-Object{$_.BaseName -ne $fileName}|ForEach-Object{
        Write-HostFormatted "Xpand Version Patching $($_.FullName) ($version)" -ForegroundColor Purple
        Update-Version $_.FullName $version $fileName $snk
    }
}
Write-HostFormatted "Copying Updates" -Section

Get-ChildItem $updates |ForEach-Object{
    Copy-Item $_.FullName "$monoPath\$($_.Name)" -Force
}
Remove-Item "$SourcePath\bin\Nupkg" -Force -Recurse
Get-ChildItem "$SourcePath\bin\runtimes" -Exclude "Win"|Remove-Item -Force -Recurse
Get-ChildItem "$SourcePath\bin\runtimes" "x64" -Recurse|Remove-Item -Force -Recurse

# Compress-Archive "$sourcePath\bin" -CompressionLevel Fastest -DestinationPath "$sourcepath\artifacts\bin.zip"