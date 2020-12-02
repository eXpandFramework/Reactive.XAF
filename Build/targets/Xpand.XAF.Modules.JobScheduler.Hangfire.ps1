param(
    $TargetPath = "C:\Work\eXpandFramework\Issues\Solution10\Solution10.Module\bin\Debug\Solution10.Module.dll",
    $ProjectPath = "DoNotRemove"
)
Write-HostFormatted SingHangfire -Section
$nugetPackagesFolder = "$env:USERPROFILE\.nuget\packages"
$versionConverterPath=(Get-ChildItem "$nugetPackagesFolder\xpand.versionconverter"|Select-Object -Last 1).FullName
[System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes("$versionConverterPath\build\Mono.Cecil\Mono.Cecil.dll")) | Out-Null
"Xpand.XAF.Modules.JobScheduler","hangfire*"|ForEach-Object{
    Get-ChildItem $nugetPackagesFolder $_|ForEach-Object{
        Get-ChildItem $_.FullName "$($_.Name).dll" -Recurse|Where-Object{$_.FullName -notmatch "netstandard1.3"}|ForEach-Object{
            $readerParams = New-Object Mono.Cecil.ReaderParameters
            $readerParams.ReadWrite = $true
            [Mono.Cecil.AssemblyDefinition]$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($_.FullName, $readerParams)
            if ($asm.Name.Name -eq "hangfire.core"){
                $asm.CustomAttributes.ToArray()|Where-Object{$_.attributetype.Name -eq "InternalsVisibleToAttribute"}|ForEach-Object{
                    $asm.CustomAttributes.Remove($_)
                }
                $asm.Write()
            }
            if (!$asm.Name.PublicKeyToken) {
                Write-HostFormatted $_.FullName -Debug
                $writeParams = New-Object Mono.Cecil.WriterParameters
                $key = [System.IO.File]::ReadAllBytes("$versionConverterPath\build\Xpand.snk")
                $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
                $asm.Write($writeParams)
            }
            $hangfireReference = $asm.MainModule.AssemblyReferences | Where-Object { $_.Name -like "Hangfire.*" }
            if ($hangfireReference -and !$hangfireReference.PublicKeyToken) {
                $publicKeyToken = (([Mono.Cecil.AssemblyNameReference]::Parse("$($hangfireReference.name), Version=$($hangfireReference.Version), Culture=$($hangfireReference.Culture), PublicKeyToken=c52ffed5d5ff0958"))).PublicKeyToken
                $hangfireReference.PublicKeyToken = $publicKeyToken
                $asm.Write()
            }
            $asm.dispose()
        }
    }
}


#copysymbols
$pdbname = "$((Get-ChildItem $PSScriptRoot *.targets).BaseName).pdb"
$targetDir=[System.IO.Path]::GetDirectoryName($TargetPath)
$pdbPath=Get-ChildItem $PSScriptRoot\..\lib *.pdb -Recurse
if (!(Test-Path "$targetDir\$pdbname")){
    $pdbPath|Copy-Item -Destination $targetDir
}
else{
    if (!(Compare-Object $pdbPath "$targetDir\$pdbname" -Property Length, LastWriteTime)){
        Copy-Item $pdbPath "$targetDir\$pdbname" -Force
    }
}