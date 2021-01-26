if ($SkipVersioning){
    return
}
Write-HostFormatted "Reset modified assemblyInfo" -Section
"AssemblyInfo.cs", "/.nuspec" | Get-GitDiff | Restore-GitFile
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release

$lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
$newVersion = (Update-Version $lastVersion -Revision)
$lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
if ($Branch -eq "master") {
    $lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $newVersion = (Update-Version $lastVersion -Build)
}
else {
    
    if ($lastOfficialVersion -gt $lastVersion) {
        $build=$lastOfficialVersion.Build
        if ($lastVersion.Build -gt $lastOfficialVersion.Build){
            $build=$lastVersion.Build
        }
        $newVersion = [version]"$($lastOfficialVersion.Major).$($lastOfficialVersion.Minor).$build.1"
    }
}

if (!$sourcePath) {
    $sourcePath = "$PathToScript\..\src"
}
Set-VsoVariable build.updatebuildnumber "$newVersion-$CustomVersion"
Update-AssemblyInfoVersion $newVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs"
[xml]$Nuspec=Get-XmlContent "$PSScriptRoot\..\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec"
$Nuspec.package.metadata.version=$newVersion
$Nuspec.Save("$PSScriptRoot\..\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec")
return
