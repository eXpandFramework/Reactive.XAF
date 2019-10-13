param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName = "apobekiaris",
    $Pass = $env:GithubPass,
    $DXApiFeed  ,
    $artifactstagingdirectory,
    $bindirectory,
    $AzureToken = (Get-AzureToken),
    $PastBuild,
    $CustomVersion,
    $WhatIf = $false
    
)
if ($PastBuild){
    return
}
"CustomVersion=$CustomVersion"
$ErrorActionPreference = "Stop"
$regex = [regex] '(\d*\.\d*)'
$result = $regex.Match($CustomVersion).Groups[1].Value;
& "$SourcePath\go.ps1" -InstallModules
if (!$result){
    $CustomVersion="latest"
}
else{
    $latestMinors=Get-NugetPackageSearchMetadata -Name DevExpress.Xpo -AllVersions -Source $DXApiFeed|ForEach-Object{
        $v=$_.Identity.Version.Version
        [PSCustomObject]@{
            Version = $v
            Minor="$($v.Major).$($v.Minor)"
        }
    }|Group-Object -Property Minor|ForEach-Object{
        $_.Group|Select-Object -First 1 -ExpandProperty Version
    }|Sort-Object -Descending
    "latestMinors:"
    $latestMinors
    $CustomVersion=$latestMinors|Where-Object{"$($_.Major).$($_.Minor)" -eq $result}
}
"CustomVersion=$CustomVersion"

$defaulVersion=Get-Content "$SourcePath\go.ps1"|Select-String dxVersion|Select-Object -First 1|ForEach-Object{
    $regex = [regex] '"([^"]*)'
    $regex.Match($_).Groups[1].Value;
}

if ($Branch -eq "master") {
    $bArgs = @{
        packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
        tasklist       = "release"
        Release        = $true
    }
    & $SourcePath\go.ps1 @bArgs
    return    
}

if (!(Get-Module VSTeam -ListAvailable)) {
    Install-Module VSTeam -Force
}
Set-VSTeamAccount -Account eXpandDevOps -PersonalAccessToken $AzureToken
$officialPackages = Get-XpandPackages -PackageType XAF -Source Release
$labPackages = Get-XpandPackages -PackageType XAF -Source Lab
$DXVersion = Get-DevExpressVersion 
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
Write-Host "localPackages:" -f blue
$localPackages | Out-String
$publishedPackages = $labPackages | ForEach-Object {
    $publishedName = $_.Id
    $localPackages | Where-Object { $_.Id -eq $publishedName }
}
Write-Host "publishedPackages:" -f blue
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
Write-Host "newPackages:" -f blue
$newPackages

$labBuild = Get-VSTeamBuild -ResultFilter succeeded -ProjectName expandframework -top 1 -StatusFilter completed -Definitions 23

$yArgs = @{
    Owner         = $GitHubUserName
    Organization  = "eXpandFramework"
    Repository    = "DevExpress.XAF"
    Branch        = $Branch
    Pass          = $Pass
    Packages      = $publishedPackages 
    SourcePath    = $SourcePath
    CommitsSince  = $labBuild.finishTime
    ExcludeFilter = "Test"
    WhatIf        = $WhatIf
}
if ($newPackages) {
    $yArgs.Packages += $newPackages
}
Write-Host "End-Packages:" -f blue
$yArgs.Packages 
$updateVersion = Update-NugetProjectVersion @yArgs | Select-Object -Skip 1
"updateVersion=$updateVersion"
$reactiveVersionChanged = $updateVersion | Select-String "Xpand.XAF.Modules.Reactive"
"reactiveVersionChanged=$reactiveVersionChanged"
if ($reactiveVersionChanged) {
    $reactiveModules = Get-ChildItem "$sourcePath\src\Modules" *.csproj -Recurse | ForEach-Object {
        [xml]$csproj = Get-Content $_.FullName
        $packageName = $_.BaseName
        $csproj.project.itemgroup.reference.include | Where-Object { $_ -eq "Xpand.XAF.Modules.Reactive" } | ForEach-Object { $packageName }
    }
    "reactiveModules:"
    $reactiveModules | Write-Host
    Get-ChildItem "$sourcePath\src\Modules" *.csproj -Recurse | ForEach-Object {
        Update-AssemblyInfo $_.DirectoryName -Revision
    }
}
$taskList="Release"
if ($customVersion -eq "latest"){
    $defaulVersion=$DXVersion
}
elseif ($CustomVersion){
    $taskList="TestsRun"
    $defaulVersion=$CustomVersion
}

$bArgs = @{
    packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist       = $tasklist
    Version        = $defaulVersion
    CustomVersion  = !($customVersion)
}
"bArgs:"
$bArgs|Out-String

& $SourcePath\go.ps1 @bArgs

"$SourcePath\Bin\Nupkg", "$SourcePath\Bin\Nuspec" | ForEach-Object {
    Get-ChildItem $_ -Recurse | ForEach-Object {
        Move-Item $_.FullName -Destination $artifactstagingdirectory
    }
}

