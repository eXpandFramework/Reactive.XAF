
$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
Write-HostFormatted "labPackages" -Section
$labPackages | Out-String

$localPackages = (Get-ChildItem "$sourcePath\src\Modules" "*.csproj" -Recurse) + (Get-ChildItem "$sourcePath\src\Extensions" "*.csproj" -Recurse) | ForEach-Object {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $localVersion = Get-XpandVersion -XpandPath $_.DirectoryName -module $name
    $nextVersion = Get-XpandVersion -Next -module $name -OfficialPackages $officialPackages -LabPackages $labPackages -DXVersion $DXVersion
    if (!$nextVersion -or $localVersion -gt $nextVersion) {
        $nextVersion = $localVersion
    }
    if ($Branch -eq "lab") {
        $v = (New-Object System.Version($nextVersion))
        if ($v.Revision -lt 1) {
            $nextVersion = New-Object System.Version($v.Major, $v.Minor, $v.Build, 1)
        }
    }
    [PSCustomObject]@{
        Id           = $name
        NextVersion  = $nextversion
        LocalVersion = $localVersion
    }
}
Write-HostFormatted "localPackages:" -Section
$localPackages | Out-String
$publishedPackages = $labPackages | ForEach-Object {
    $publishedName = $_.Id
    $localPackages | Where-Object { $_.Id -eq $publishedName }
}
Write-HostFormatted "publishedPackages:" -Section
$publishedPackages | Out-String
$newPackages = $localPackages | Where-Object { !(($publishedPackages | Select-Object -ExpandProperty Id) -contains $_.Id) } | ForEach-Object {
    $localVersion = New-Object System.Version($_.LocalVersion)
    $nextVersion = New-Object System.Version($localVersion.Major, $localVersion.Minor, $localVersion.Build)
    [PSCustomObject]@{
        Id           = $_.Id
        NextVersion  = $nextVersion
        LocalVersion = $localVersion
    }
}
Write-HostFormatted "newPackages:" -Section
$newPackages | Out-String

$cred=@{
    Project="expandFramework"
    Organization="eXpandDevOps"
    Token=$AzureToken
}
$labBuild = Get-AzBuilds -Result succeeded -Status completed -Definition DevExpress.XAF-Lab @cred|
where-object{$_.status -eq "completed"}|Select-Object -First 1
Write-HostFormatted "labBuild" -Section
$labBuild.buildNumber
if (!$labBuild ){
    throw "lab build not found"
}
if ($labBuild.result -ne 'succeeded'){
    throw "labebuild result is $($labBuild.result)"
}
$yArgs = @{
    Organization  = "eXpandFramework"
    Repository    = "DevExpress.XAF"
    Branch        = $Branch
    Token         = $GitHubToken
    Packages      = $publishedPackages 
    SourcePath    = $SourcePath
    CommitsSince  = $labBuild.finishTime
    ExcludeFilter = "Test"
}
if ($newPackages) {
    $yArgs.Packages += $newPackages
}
Write-HostFormatted "Changed-Packages:" -Section
$yArgs.Packages | Out-String 
if ($Branch -eq "lab") {
    Get-ChildItem $sourcePath *.csproj -Recurse | ForEach-Object {
        $pName = $_.BaseName
        $pDir = $_.DirectoryName
        $yArgs.Packages | Where-Object { $_.id -eq $pName } | ForEach-Object {
            $nextVersion = $_.NextVersion
            if ($nextVersion.Revision -gt -1) {
                $revision = [int]$nextVersion.Revision - 1
                $nowVersion = New-Object version ($nextVersion.Major, $nextVersion.Minor, $nextVersion.Build, $revision)
                Write-HostFormatted "Update $pName version to current published $nowVersion" -ForegroundColor Magenta
                Update-AssemblyInfoVersion $nowVersion $pDir
            }
        }
    }
}
Write-HostFormatted "updateVersion comparing local/remote differences:" -Section
$updateVersion=@(Update-NugetProjectVersion @yArgs -Verbose)

# $releasepackages=Get-XpandPackages Release XAFAll
$publishedPackages=Get-XpandPackages Lab XAFAll
$localPackages=(Get-ChildItem "$sourcePath\src\Modules" "*.csproj" -Recurse) + (Get-ChildItem "$sourcePath\src\Extensions" "*.csproj" -Recurse)|ForEach-Object{
    [PSCustomObject]@{
        Id = $_.BaseName
        Version=[version](Get-AssemblyInfoVersion "$($_.DirectoryName)\Properties\assemblyinfo.cs")
        File=$_
    }
}
Write-HostFormatted "Matching remote build versions" -Section
$localPackages|ForEach-Object{
    $localpackage=$_
    $publishedPackage=$publishedPackages|Where-Object{$_.id -eq $localpackage.id}
    $publishedVersion=([version](Get-VersionPart $publishedPackage.Version Build))
    $local=([version](Get-VersionPart $localpackage.Version Build))
    if ($local -ne $publishedVersion){
        $remoteversion="$(Get-VersionPart $publishedVersion Build).$(($publishedVersion.Revision+1))"
        write-warning "$($localPackage.Id) release build version ($remoteVersion) is different than local ($local)"
        $updateVersion+=$localPackage.File.BaseName
    }
}
$updateVersion|Sort-Object -Unique