Write-HostFormatted "Reset modified assemblyInfo" -Section
"AssemblyInfo.cs","/.nuspec"|Get-GitDiff |Restore-GitFile
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release
if ($Branch -eq "master"){
    $updateVersion=@()
    $newPackages=@(Get-ChildItem "$sourcePath\build\Nuspec" -Exclude "Xpand.TestsLib.nuspec"|ForEach-Object{
        [xml]$nuspec=Get-XmlContent $_.FullName
        $nuspec.package.metadata.id
    }|Where-Object{ $_ -notin $officialPackages.id})
    if ($newPackages){
        Get-MSBuildProjects $sourcePath|ForEach-Object{
            if ($_.BaseName -in $newPackages){
                Update-AssemblyInfo "$($_.DirectoryName)\Properties" -Build
                $updateVersion+=$_.BaseName
            }
        }
    }
    
    Get-ChildItem $sourcePath *.csproj -Recurse |ForEach-Object{
        $project=$_
        $assemblyInfoPath="$($project.DirectoryName)\Properties\AssemblyInfo.cs"
        if (Test-Path $assemblyInfoPath){
            $labPackage=$labPackages|Where-Object{$_.Id -eq $project.BaseName}
            if ($labPackage){
                $projectVersion=[version](Get-AssemblyInfoVersion $assemblyInfoPath)
                if ((Get-VersionPart $projectVersion Build) -ne (Get-VersionPart $labPackage.version Build) -or 
                    ($projectVersion -lt $labPackage.Version) -or ($projectVersion.Revision -gt 0)){
                    $updateVersion+=$_.BaseName
                    Update-AssemblyInfo $assemblyInfoPath -Build
                }   
            }
        }
    }
    $updateVersion
    return
}
Write-HostFormatted "Semester version check"  -Section



Write-HostFormatted "labPackages"  -Section
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
        File=$_
    }
}
Write-HostFormatted "localPackages:"  -Section
$localPackages | Out-String


$publishedPackages = $labPackages | ForEach-Object {
    $publishedName = $_.Id
    $localPackages | Where-Object { $_.Id -eq $publishedName }
}
Write-HostFormatted "publishedPackages:"  -Section
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
Write-HostFormatted "newPackages:"  -Section
$newPackages | Out-String

$cred = @{
    Project      = "expandFramework"
    Organization = "eXpandDevOps"
    Token        = $AzureToken
}
$labBuild = Get-AzBuilds -Result succeeded -Status completed -Definition PublishNugets-DevExpress.XAF @cred |
Where-Object { $_.status -eq "completed" } | Select-Object -First 1
Write-HostFormatted "labBuild" -Section
$labBuild.buildNumber
if (!$labBuild ) {
    throw "lab build not found"
}
if ($labBuild.result -ne 'succeeded') {
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
Write-HostFormatted "yArgs" -Section
$yArgs | Out-String 

Write-HostFormatted "updateVersion comparing local/remote differences:" -Section

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
Write-HostFormatted "CHECK IF REMOTE INDEX IS DELAYED" -Section
$currentSha=Get-GitLastSha -repoGitUrl .
$repoUri=(Get-XpandRepository -Name DevExpress.XAF -Uri)

$remoteSha=Get-GitLastSha -repoGitUrl $repoUri -Branch lab
if ($currentSha -ne $remoteSha){
    throw "REMOTE INDEX IS DELAYED. PLEASE PUSH"
}
$updateVersion = @(Update-NugetProjectVersion @yArgs -Verbose)
Write-HostFormatted "Updated packages" -Section
$updateVersion

$publishedPackages = Get-XpandPackages Lab XAFAll
$localPackages = (Get-ChildItem "$sourcePath\src\Modules" "*.csproj" -Recurse) + (Get-ChildItem "$sourcePath\src\Extensions" "*.csproj" -Recurse) | ForEach-Object {
    [PSCustomObject]@{
        Id      = $_.BaseName
        Version = [version](Get-AssemblyInfoVersion "$($_.DirectoryName)\Properties\assemblyinfo.cs")
        File    = $_
    }
}
Write-HostFormatted "Checking if local build version increase" -Section
$localPackages | ForEach-Object {
    $localpackage = $_
    $publishedPackage = $publishedPackages | Where-Object { $_.id -eq $localpackage.id }
    if ($publishedPackage){
        $publishedVersion = ([version](Get-VersionPart $publishedPackage.Version Build))
        $local = ([version](Get-VersionPart $localpackage.Version Build))
        if ($local -ne $publishedVersion) {
            $remoteversion = "$(Get-VersionPart $publishedVersion Build).$(($publishedVersion.Revision+1))"
            Write-Warning "$($localPackage.Id) release build version ($remoteVersion) is different than local ($local)"
            $updateVersion += $localPackage.File.BaseName
        }
    }
}
if ($updateVersion) {
    Write-HostFormatted "Collect related assemblies:" -Section
    $packageDeps=Get-XpandPackages Lab XAFAll|Where-Object{$_.id -notlike "*all*"}|Invoke-Parallel -Script{
        [PSCustomObject]@{
            Id = $_.id
            Deps=(Get-NugetPackageDependencies $_.Id -Source (Get-PackageFeed -Xpand) -Filter "Xpand.*" -Recurse)
        }
    }
    $relatedPackages=$updateVersion | ForEach-Object {
        $updatedPackage=$_
        $packageDeps|Where-Object{$updatedPackage -in $_.Deps.Id }|ForEach-Object{
            try {
                $dependency=$_
                $localpackage=($localPackages|Where-Object{$_.Id -eq $dependency.Id})
                $newVersion="$(Update-Version $localpackage.Version -Revision)"
                Update-AssemblyInfoVersion -version $newVersion -path "$($localpackage.File.DirectoryName)\properties\assemblyinfo.cs"
                $dependency
            }
            catch {
                $_
            }
        }
    }
    $relatedPackages.Id
    $updateVersion +=$relatedPackages.Id|Sort-Object -Unique
}
$updateVersion+=$newPackages.Id|Sort-Object -Unique

