if ($SkipVersioning){
    return
}

Write-HostFormatted "Reset modified assemblyInfo" -Section
"AssemblyInfoVersion.cs", "/.nuspec" | Get-GitDiff | Restore-GitFile
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release

$lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
$lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
if ($lastOfficialVersion -gt $lastVersion){
    $lastVersion=$lastOfficialVersion
}

if ($Branch -eq "master") {
    # $srcVersion=[version](Get-AssemblyInfoVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs")
    # if ($srcVersion -gt $lastVersion){
    #     $lastVersion=$srcVersion
    # }
    # $newVersion = (Update-Version $lastVersion -Revision)
    # $lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $newVersion = (Update-Version $lastVersion -Build)
}
elseif (!$SkipVersioning) {
    $srcVersion=[version](Get-AssemblyInfoVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs")
    if ($srcVersion -gt $lastVersion){
        $newVersion=$srcVersion
    }
    else{
        $newVersion=Update-Version $lastVersion -Revision
    }
}

if (!$sourcePath) {
    $sourcePath = "$PathToScript\..\src"
}
Set-VsoVariable build.updatebuildnumber "$newVersion-$CustomVersion"
Update-AssemblyInfoVersion $newVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs"
"$PSScriptRoot\..\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec","$PSScriptRoot\..\Tools\Xpand.XAF.ModelEditor\Build\Xpand.XAF.ModelEditor.nuspec"|ForEach-Object{
    [xml]$Nuspec=Get-XmlContent $_
    $Nuspec.package.metadata.version=$newVersion
    $Nuspec.Save($_)
}

return
