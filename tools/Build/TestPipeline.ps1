using namespace Mono.Cecil
using namespace System.Reflection
using namespace System.IO
param(
    $SourcePath = [Path]::GetFullPath("$PSScriptRoot\..\..")
)
$ErrorActionPreference = "Stop"

$buildNumber = $env:build_BuildNumber
$buildNumber += $env:Build_TriggeredBy_DefinitionName
& "$SourcePath\go.ps1" -InstallModules
if ($buildNumber){
    Get-AzArtifact -Definition DevExpress.XAF-Lab -ArtifactName bin -Outpath "$SourcePath"
    Get-AzArtifact -Definition DevExpress.XAF-Lab -ArtifactName Tests -Outpath "$SourcePath"
}

function UpdateVersion {
    param (
        $_,
        $monoPath,
        [FileInfo[]]$assemblyList
    )
    if (Test-Path "$monoPath\$($_.Name)") {
        Write-HostFormatted "updating $monoPath\$($_.Name)" -Section
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$monoPath\$($_.Name)").FileVersion
    
        $assemblyResolver = [AssemblyResolver]::new($assemblyList)
        $readerParams = New-Object ReaderParameters
        $readerParams.AssemblyResolver = $assemblyResolver
        $readerParams.ReadWrite = $true
        $moduleDefinition = [ModuleDefinition]::ReadModule($_.FullName, $readerParams)
        $attribute = $moduleDefinition.Assembly.CustomAttributes | Where-Object { $_.AttributeType.Name -eq "AssemblyFileVersionAttribute" }
        $attribute.ConstructorArguments.RemoveAt(0)
        $newArg = New-Object Mono.Cecil.CustomAttributeArgument($moduleDefinition.TypeSystem.String, "$version")
        $attribute.ConstructorArguments.Add($newArg)
        $currentVersion = $moduleDefinition.Assembly.Name.Version
        $moduleDefinition.Assembly.Name.Version = $version    
        if ($currentVersion -ne $version) {
            $moduleDefinition.Write() 
            Write-HostFormatted "$($_.BaseName) version changed from $currentVersion to $version" -ForegroundColor Magenta
        }
    
        $moduleDefinition.Dispose()
    }
}

Invoke-Script {
    Write-Verbose -Verbose "##vso[build.updatebuildnumber]$buildNumber"
    
    $monoPath = "$SourcePath\bin\"
    Write-HostFormatted "monoPath=$monoPath" -Section
    Get-ChildItem $monoPath
    . $SourcePath\tools\Xpand.VersionConverter\functions.ps1
    Install-MonoCecil $monopath
    . $SourcePath\tools\Xpand.VersionConverter\AssemblyResolver.ps1

    $updates = [System.IO.Path]::GetFullPath("$SourcePath\bin\updates")
    Remove-Item $updates -Recurse -Force -ErrorAction SilentlyContinue
    New-Item $updates -ItemType Directory -Force

    Write-HostFormatted  "Downloading into $updates" -Section

    Get-AzStorageBlob -Container xpandbuild -Context (Get-AzStorageAccount|Where-Object{$_.StorageAccountName -eq "xpandbuildblob"}).Context | Get-AzStorageBlobContent -Destination $updates -Force | Out-Null
    Write-HostFormatted "Download Finished" -ForegroundColor Green

    [version]$dxVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(((Get-ChildItem $monoPath *DevExpress*.dll).FullName | Select-Object -First 1)).FileVersion
    Write-HostFormatted "dxVersion=$dxVersion" -ForegroundColor Green
    $snk = "$Sourcepath\src\Xpand.key\Xpand.snk"
    $allAssemblies = Get-ChildItem $monopath *.dll
    Get-ChildItem $updates *.dll | ForEach-Object {
        Write-HostFormatted "DevExpress Version Patching $($_.BaseName) $($dxVersion)" -Section
        Switch-ReferencesVersion $_.FullName $dxVersion DevExpress* $snk $allAssemblies
        UpdateVersion $_ $monoPath $allAssemblies
    }
}
Invoke-Script {
    Get-ChildItem $monoPath "*Xpand.*.dll" | ForEach-Object {
        $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion
        $fileName = $_.BaseName
        Write-HostFormatted "Xpand Version Patching $($_.FullName) ($version)" -ForegroundColor Magenta
        Get-ChildItem $updates *.dll | Where-Object { $_.BaseName -ne $fileName } | ForEach-Object {
            
            Switch-ReferencesVersion $_.FullName $version $fileName $snk $allAssemblies
        }
    }
}
Invoke-Script{
    Write-HostFormatted "Copying Updates" -Section

    Get-ChildItem $updates | ForEach-Object {
        Copy-Item $_.FullName "$monoPath\$($_.Name)" -Force -verbose
    }
    write-hostformatted "Removing Nupkg" -ForegroundColor Magenta
    Remove-Item "$SourcePath\bin\Nupkg" -Force -Recurse -ErrorAction SilentlyContinue
    write-hostformatted "Copying grpc" -ForegroundColor Magenta
    Get-ChildItem "$SourcePath\bin\runtimes" -Exclude "Win" | Remove-Item -Force -Recurse
    Get-ChildItem "$SourcePath\bin\runtimes" "x64" -Recurse | Remove-Item -Force -Recurse

    
}
$patchingSkipped=((Get-ChildItem $updates).count -eq 0)
Write-HostFormatted "Patching skipped $patchingSkipped"
Set-VsoVariable "NewArtifacts" !$patchingSkipped
# Compress-Archive "$sourcePath\bin" -CompressionLevel Fastest -DestinationPath "$sourcepath\artifacts\bin.zip"