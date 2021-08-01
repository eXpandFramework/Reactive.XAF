param(
    $TargetPath ,
    $ProjectPath = "DoNotRemove",
    $nugetPackagesFolder="$env:USERPROFILE\.nuget\packages"
)

Write-Host ------- Patch Hangfire------------

# Get-ChildItem $nugetPackagesFolder *hangfire*|Remove-Item -Recurse -Force
# XpandPwsh\Invoke-PaketRestore -Strict
$versionConverterPath = (Get-ChildItem "$nugetPackagesFolder\xpand.versionconverter" | Select-Object -Last 1).FullName
$netstandard=get-item "$nugetPackagesFolder\netstandard.library\2.0.3\build\netstandard2.0\ref\netstandard.dll"
[System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes("$versionConverterPath\build\Mono.Cecil\Mono.Cecil.dll")) | Out-Null
. "$versionConverterPath\build\AssemblyResolver.ps1"
"Xpand.XAF.Modules.JobScheduler", "hangfire*" | ForEach-Object {
    Get-ChildItem $nugetPackagesFolder $_ | ForEach-Object {
        Get-ChildItem $_.FullName "$($_.Name).dll" -Recurse | ForEach-Object {
            if ($_.FullName -notmatch "(netstandard1.3|net45)\\Hangfire.SqlServer"){
                $readerParams = New-Object Mono.Cecil.ReaderParameters
                $readerParams.ReadWrite = $true
                $assemblyList=$netstandard
                $assemblyResolver = [AssemblyResolver]::new($assemblyList)
                $readerParams.AssemblyResolver = $assemblyResolver
                [Mono.Cecil.AssemblyDefinition]$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($_.FullName, $readerParams)
                
                if ($asm.Name.Name -eq "hangfire.core") {
                    $asm.CustomAttributes.ToArray() | Where-Object { $_.attributetype.Name -eq "InternalsVisibleToAttribute" } | ForEach-Object {
                        $asm.CustomAttributes.Remove($_)|Out-Null
                    }
                }
                
                if (!$asm.Name.PublicKeyToken) {
                    $writeParams = New-Object Mono.Cecil.WriterParameters
                    Write-Host $_.FullName 
                    $key = [System.IO.File]::ReadAllBytes("$versionConverterPath\build\Xpand.snk")
                    $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
                    $hangfireReference = $asm.MainModule.AssemblyReferences | Where-Object { $_.Name -like "Hangfire.*" }
                    if ($hangfireReference -and !$hangfireReference.PublicKeyToken) {
                        $publicKeyToken = (([Mono.Cecil.AssemblyNameReference]::Parse("$($hangfireReference.name), Version=$($hangfireReference.Version), Culture=$($hangfireReference.Culture), PublicKeyToken=c52ffed5d5ff0958"))).PublicKeyToken
                        $hangfireReference.PublicKeyToken = $publicKeyToken
                    }
                    $asm.Write($writeParams)
                }
                
                $asm.dispose()
            }
        }
    }
}

if ($TargetPath) {
    $pdbname = "$((Get-ChildItem $PSScriptRoot *.targets).BaseName).pdb"
    $targetDir = [System.IO.Path]::GetDirectoryName($TargetPath)
    $pdbPath = Get-ChildItem $PSScriptRoot\..\lib *.pdb -Recurse
    if (!(Test-Path "$targetDir\$pdbname")) {
        $pdbPath | Copy-Item -Destination $targetDir
    }
    else {
        if (!(Compare-Object $pdbPath "$targetDir\$pdbname" -Property Length, LastWriteTime)) {
            Copy-Item $pdbPath "$targetDir\$pdbname" -Force
        }
    }
}
