if ($SkipVersioning){
    return
}

Write-HostFormatted "Reset modified assemblyInfo" -Section
"AssemblyInfo.cs", "/.nuspec" | Get-GitDiff | Restore-GitFile
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release
 
$lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version

$lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
if ($Branch -eq "master") {
    $srcVersion=[version](Get-AssemblyInfoVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs")
    if ($srcVersion -gt $lastVersion){
        $lastVersion=$srcVersion
    }
    $newVersion = (Update-Version $lastVersion -Revision)
    $lastVersion = ($labPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $lastOfficialVersion = ($officialPackages | Where-Object { $_.id -eq "Xpand.XAF.Core.All" }).Version
    $newVersion = (Update-Version $lastVersion -Build)
}
elseif (!$SkipVersioning) {
    if (($lastOfficialVersion -gt $lastVersion) -or ($lastVersion.Build -lt (Get-DevExpressVersion).Build)) {
        $build=$lastOfficialVersion.Build
        if ($lastVersion.Build -gt $lastOfficialVersion.Build){
            $build=$lastVersion.Build
        }
        $srcVersion=[version](Get-AssemblyInfoVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs")
        $major=$lastOfficialVersion.Major
        $minor=$lastOfficialVersion.Minor
        if ($minor -lt $srcVersion.Minor){
            $minor=$srcVersion.minor
            $build=0
        }
        $newVersion = [version]"$major.$minor.$build.1"
    }
    else{
        $srcVersion=[version](Get-AssemblyInfoVersion "$PSScriptRoot\..\src\Common\AssemblyInfoVersion.cs")
        if ($srcVersion -gt $lastVersion){
            $lastVersion=$srcVersion
        }
        
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
