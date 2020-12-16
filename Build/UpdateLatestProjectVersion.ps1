Write-HostFormatted "Reset modified assemblyInfo" -Section
"AssemblyInfo.cs", "/.nuspec" | Get-GitDiff | Restore-GitFile
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release

$lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
$newVersion = (Update-Version $lastVersion -Revision)
if ($Branch -eq "master") {
    $lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $newVersion = (Update-Version $lastVersion -Build)
}
else {
    $lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    if ($lastOfficialVersion -gt $lastVersion) {
        $newVersion = [version]"$($lastOfficialVersion.Major).$($lastOfficialVersion.Minor).$($lastVersion.Build).1"
    }
}

if (!$sourcePath) {
    $sourcePath = "$PathToScript\..\src"
}
Set-VsoVariable build.updatebuildnumber "$newVersion-$CustomVersion"
Update-AssemblyInfoVersion $newVersion "$sourcePath\src\Common\AssemblyInfoVersion.cs"


return
