param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName = "apobekiaris",
    $Token = $env:GitHubToken,
    $DXApiFeed = $env:DxFeed,
    $artifactstagingdirectory,
    $bindirectory,
    $AzureToken = $env:AzureToken,
    $PastBuild,
    $CustomVersion,
    $latest,
    [switch]$Run
)
if ($CustomVersion -and !$Run) {
    $goArgs = @{
        Branch                   = $env:Build_SourceBranchName
        SourcePath               = $env:System_DefaultworkingDirectory
        GitHubUserName           = $env:GitHubUserName
        Token                    = $Token
        DXApiFeed                = $DXApiFeed
        ArtifactStagingDirectory = $env:build_artifactstagingdirectory
        BinDirectory             = "$env:System_DefaultworkingDirectory\bin"
        AzureToken               = $AzureToken
        PastBuild                = $env:PastBuild
        CustomVersion            = $env:Build_DefinitionName
        Convert                  = $Convert
        Run                      = $true
    }
    "goArgs:"
    $goArgs | Out-String
    & "$PSScriptRoot\BuildPipeline.ps1" @goArgs
    return
}

"PastBuild=$PastBuild"
"latest=$latest"
if ($PastBuild -and $PastBuild -ne "false") {
    return
}
"CustomVersion=$CustomVersion"
$ErrorActionPreference = "Stop"
$regex = [regex] '(\d{2}\.\d*)'
$result = $regex.Match($CustomVersion).Groups[1].Value;
& "$SourcePath\go.ps1" -InstallModules
$stage = "$SourcePath\buildstage"
Copy-Item "$SourcePath\paket.lock" "$SourcePath\paket.lock1"
Remove-Item $stage -force -recurse -ErrorAction SilentlyContinue
Set-Location $SourcePath
dotnet tool restore

if (!$result) {
    $CustomVersion = "latest"
}
else {
    $latestMinors = Get-LatestMinorVersion "DevExpress.ExpressApp" $DXApiFeed
    "latestMinors:"
    $latestMinors
    $CustomVersion = $latestMinors | Where-Object { "$($_.Major).$($_.Minor)" -eq $result }
}
"CustomVersion=$CustomVersion"

$defaulVersion = & "$SourcePath\Tools\Build\DefaultVersion.ps1"
if ($latest) {
    $customVersion = $defaulVersion
    "CustomVersion=$CustomVersion"
}


$taskList = "ReleaseModules"
if ($Branch -eq "master") {
    $bArgs = @{
        packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
        tasklist       = $taskList 
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
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
Write-Host "labPackages" -f Blue
$labPackages | Out-String
$DXVersion = Get-DevExpressVersion 
"DXVersion=$DXVersion"
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
$newPackages | Out-String

$labBuild = Get-VSTeamBuild -ResultFilter succeeded -ProjectName expandframework -top 1 -StatusFilter completed -Definitions 23

$yArgs = @{
    Organization  = "eXpandFramework"
    Repository    = "DevExpress.XAF"
    Branch        = $Branch
    Token         = $Token
    Packages      = $publishedPackages 
    SourcePath    = $SourcePath
    CommitsSince  = $labBuild.finishTime
    ExcludeFilter = "Test"
    Verbose       = $true
}
if ($newPackages) {
    $yArgs.Packages += $newPackages
}
Write-Host "End-Packages:" -f blue
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
                Write-Host "Update $pName version to latest $nowVersion"
                Update-AssemblyInfoVersion $nowVersion $pDir
            }
        }
    }
}

$updateVersion = Update-NugetProjectVersion @yArgs 
"updateVersion:"
$updateVersion

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
        $name = $_.baseName
        $version = $localPackages | Where-Object { $_.id -eq $name } | Select-Object -ExpandProperty NextVersion
        Update-AssemblyInfoVersion -path "$($_.DirectoryName)" -version $version
    }
}


$newVersion = $defaulVersion
if ($customVersion -eq "latest") {
    $newVersion = $DXVersion
}
elseif ($CustomVersion -and !$latest) {
    $taskList = "TestsRun"
    $newVersion = $CustomVersion
}


$bArgs = @{
    packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist       = $tasklist
    dxVersion      = $newVersion
    CustomVersion  = $newVersion -ne $defaulVersion
}
"bArgs:"
$bArgs | Out-String
$SourcePath, "$SourcePath\src\tests\all" | ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 $DXApiFeed
}
Start-XpandProjectConverter -version $newVersion -path $SourcePath -SkipInstall

if ($newVersion -ne $defaulVersion ) {
    Set-Location $SourcePath
    "PaketInstall $SourcePath (due to different Version)"
    Invoke-PaketInstall -Strict 
    
} 

& $SourcePath\go.ps1 @bArgs


Set-Location $SourcePath
$stage="$Sourcepath\buildstage"
New-Item $stage -ItemType Directory -Force
Get-ChildItem $stage -Recurse|Remove-Item -Recurse -Force
New-Item $stage\source -ItemType Directory -Force
Set-Location $SourcePath
Get-ChildItem $SourcePath -Exclude ".git", "bin", "buildstage" | Copy-Item -Destination $stage\source -Recurse -Force 
Get-ChildItem $stage\source -include "packages","obj","nupkg" -Recurse|Remove-Item -Recurse -Force
Set-Location $stage



New-Item "$stage\TestApplication" -ItemType Directory
Copy-Item "$Sourcepath\Bin" "$stage\Bin" -Recurse -Force
# Move-Item "$stage\Bin\TestWinApplication" "$stage\TestApplication" -Force
# Move-Item "$stage\Bin\TestWebApplication" "$stage\TestApplication" -Force
# Move-Item "$stage\Bin\AllTestWeb" "$stage\TestApplication" -Force
# Move-Item "$stage\Bin\AllTestWin" "$stage\TestApplication" -Force
Remove-Item "$stage\bin\ReactiveLoggerClient" -Recurse -Force
Copy-Item "$SourcePath\paket.lock1" "$SourcePath\paket.lock" -Force
