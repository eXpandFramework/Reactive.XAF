using namespace Mono.Cecil
using namespace System.Reflection
using namespace System.IO
param(
    $SourcePath = ([Path]::GetFullPath("$PSScriptRoot\..")),
    $AzureToken=$env:AzureToken,
    $AzureApplicationId=$env:AzApplicationId,
    $AzureTenantId=$env:AzTenantId,
    $XpandBlobOwnerSecret=$env:AzXpandBlobOwnerSecret,
    $AzStorageLookup,
    $GithubTokene
)
$ErrorActionPreference = "Stop"
# & "$SourcePath\go.ps1" -InstallModules

New-Item -Path "$SourcePath\bin\Tests" -ItemType Directory 

if ($env:Build_DefinitionName){
    # Write-host --------Expanding DxWeb-------------------
    # Expand-Archive -DestinationPath $sourcePath\bin\net461 -Path $sourcePath\bin\Tests\DXWeb.Zip -Force
    # Expand-Archive -DestinationPath $sourcePath\bin\Tests\AllTestWeb -Path $sourcePath\bin\Tests\DXWeb.Zip -Force
    # Expand-Archive -DestinationPath $sourcePath\bin\Tests\TestWebApplication\Bin -Path $sourcePath\bin\Tests\DXWeb.Zip -Force
    # Remove-Item $sourcePath\bin\Tests\DXWeb.Zip
    # Write-host --------Expanding DxWin-------------------
    # Expand-Archive -DestinationPath $sourcePath\bin\net461 -Path $sourcePath\bin\Tests\DXWin.Zip -Force
    # Expand-Archive -DestinationPath $sourcePath\bin\Tests\AllTestWin -Path $sourcePath\bin\Tests\DXWin.Zip -Force
    # Expand-Archive -DestinationPath $sourcePath\bin\Tests\TestWinApplication -Path $sourcePath\bin\Tests\DXWin.Zip -Force
    # Remove-Item $sourcePath\bin\Tests\DXWin.Zip
    # Write-host --------Expanding DxWinnetcoreapp3.1-------------------
    # Expand-Archive -DestinationPath $sourcePath\bin -Path $sourcePath\bin\Tests\DXWinnetcoreapp3.1.Zip -Force
    # Expand-Archive -DestinationPath $sourcePath\bin\Tests\AllTestWin\netcoreapp3.1 -Path $sourcePath\bin\Tests\DXWinnetcoreapp3.1.Zip -Force
    # Expand-Archive -DestinationPath $sourcePath\bin\Tests\TestWinDesktopApplication -Path $sourcePath\bin\Tests\DXWinnetcoreapp3.1.Zip -Force
    # Remove-Item $sourcePath\bin\Tests\DXWinnetcoreapp3.1.Zip
    
    
    [version]$dxVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(((Get-ChildItem $sourcePath\bin\net461 *DevExpress*.dll).FullName | Select-Object -First 1)).FileVersion
    Write-Host "dxVersion=$dxVersion" 
    Write-Verbose -Verbose "##vso[build.updatebuildnumber]$env:build_BuildNumber-$dxVersion"

    if (!$AzStorageLookup){
        return
    }
}



$buildNumber = $env:build_BuildNumber
& "$SourcePath\go.ps1" -InstallModules
if ($buildNumber){
    $env:AzureToken=$AzureToken
    $env:AzOrganization="eXpandDevops"
    $env:AzProject ="eXpandFramework"
    Write-HostFormatted "Install Az powershell" -Section
    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
    Install-Module -Name Az -AllowClobber -Scope CurrentUser    
}


Write-HostFormatted "Connecting to Azure" -Section
Connect-Az -ApplicationSecret $XpandBlobOwnerSecret -AzureApplicationId $AzureApplicationId -AzureTenantId $AzureTenantId
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

$monoPath = "$SourcePath\bin\"
Write-HostFormatted "monoPath=$monoPath" -Section
Get-ChildItem $monoPath|Format-Table
. $SourcePath\tools\commonLibs\common.ps1
# Install-MonoCecil $monopath
Use-MonoCecil
# . $SourcePath\tools\commonLibs\AssemblyResolver.ps1

$updates = [System.IO.Path]::GetFullPath("$SourcePath\bin\updates")
Remove-Item $updates -Recurse -Force -ErrorAction SilentlyContinue
New-Item $updates -ItemType Directory -Force

[version]$dxVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo(((Get-ChildItem $monoPath *DevExpress*.dll).FullName | Select-Object -First 1)).FileVersion
Write-HostFormatted "dxVersion=$dxVersion" -ForegroundColor Green

Set-VsoVariable build.updatebuildnumber "$env:build_BuildNumber-$dxVersion"

Write-HostFormatted  "Downloading into $updates" -Section
Get-AzStorageBlob -Container xpandbuild -Context (Get-AzStorageAccount|Where-Object{$_.StorageAccountName -eq "xpandbuildblob"}).Context | Get-AzStorageBlobContent -Destination $updates -Force | Out-Null
Get-ChildItem $updates
Write-HostFormatted "Download Finished" -ForegroundColor Green



$snk = "$Sourcepath\src\Xpand.key\Xpand.snk"
$allAssemblies = Get-ChildItem $monopath *.dll
# Get-ChildItem $updates *.dll | ForEach-Object {
#     Write-HostFormatted "DevExpress Version Patching $($_.BaseName) $($dxVersion)" -Section
#     Switch-AssemblyDependencyVersion $_.FullName $dxVersion DevExpress* $snk $allAssemblies
#     UpdateVersion $_ $monoPath $allAssemblies
# }
# Get-ChildItem $monoPath "*Xpand.*.dll" | ForEach-Object {
#     $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion
#     $fileName = $_.BaseName
#     Write-HostFormatted "Xpand Version Patching $($_.FullName) ($version)" -ForegroundColor Magenta
#     Get-ChildItem $updates *.dll | Where-Object { $_.BaseName -ne $fileName } | ForEach-Object {
        
#         Switch-ReferencesVersion $_.FullName $version $fileName $snk $allAssemblies
#     }
# }
Write-HostFormatted "Copying Updates" -Section

Get-ChildItem $updates | ForEach-Object {
    Copy-Item $_.FullName "$sourcePath\bin\" -Force -verbose
}
write-hostformatted "Removing Nupkg" -ForegroundColor Magenta
Remove-Item "$SourcePath\bin\Nupkg" -Force -Recurse -ErrorAction SilentlyContinue
write-hostformatted "Copying grpc" -ForegroundColor Magenta
Get-ChildItem "$SourcePath\bin\runtimes" -Exclude "Win" | Remove-Item -Force -Recurse
Get-ChildItem "$SourcePath\bin\runtimes" "x64" -Recurse | Remove-Item -Force -Recurse

$patchingSkipped=((Get-ChildItem $updates).count -eq 0)
Write-HostFormatted "Patching skipped $patchingSkipped"
Set-VsoVariable "NewArtifacts" !$patchingSkipped
# Compress-Archive "$sourcePath\bin" -CompressionLevel Fastest -DestinationPath "$sourcepath\artifacts\bin.zip"