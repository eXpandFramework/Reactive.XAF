param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:DxFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken=$env:AzDevopsToken,
    $CustomVersion=$env:Build_DefinitionName,
    $latest
)

if (!(Get-Module eXpandFramework -ListAvailable)){
    $env:AzDevopsToken=$AzureToken
    $env:AzOrganization="eXpandDevOps"
    $env:AzProject ="eXpandFramework"
}

"latest=$latest"
"CustomVersion=$CustomVersion"
$ErrorActionPreference = "Stop"
$regex = [regex] '(\d{2}\.\d*)'
$result = $regex.Match($CustomVersion).Groups[1].Value;
& "$SourcePath\go.ps1" -InstallModules
$stage = "$SourcePath\buildstage"

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


$taskList = "Build"
if ($Branch -eq "master") {
    $bArgs = @{
        packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
        tasklist       = $taskList 
        Release        = $true
    }
    & $SourcePath\go.ps1 @bArgs
    return    
}


$officialPackages = Get-XpandPackages -PackageType XAFAll -Source Release
$labPackages = Get-XpandPackages -PackageType XAFAll -Source Lab
Write-HostFormatted "labPackages" -Section
$labPackages | Out-String

$DXVersion = Get-DevExpressVersion 
Write-HostFormatted "DXVersion=$DXVersion" -ForegroundColor Magenta
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
$labBuild
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
Write-HostFormatted "updateVersion:" -Section
Update-NugetProjectVersion @yArgs 


$newVersion = $defaulVersion
if ($customVersion -eq "latest") {
    $newVersion = $DXVersion
}
elseif ($CustomVersion -and !$latest) {
    $newVersion = $CustomVersion
}


$bArgs = @{
    packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist       = $tasklist
    dxVersion      = $newVersion
    CustomVersion  = $newVersion -ne $defaulVersion
    ChangedModules = $updateVersion
}
Write-HostFormatted "bArgs:" -Section
$bArgs | Out-String
$SourcePath| ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 $DXApiFeed
}
Write-HostFormatted "Start-ProjectConverter $newversion"  -Section
Start-XpandProjectConverter -version $newVersion -path $SourcePath -SkipInstall

if ($newVersion -ne $defaulVersion ) {
    Set-Location "$SourcePath"
    "PaketRestore $SourcePath"
    try {
        dotnet paket restore --fail-on-checks
        if ($LASTEXITCODE) {
            throw     
        }
    }
    catch {
        "PaketRestore Failed"
        "PaketInstall $SourcePath (due to different Version)"
        Invoke-PaketInstall -Strict
    }
} 

& $SourcePath\go.ps1 @bArgs


Set-Location $SourcePath
$stage = "$Sourcepath\buildstage"
New-Item $stage -ItemType Directory -Force
Get-ChildItem $stage -Recurse | Remove-Item -Recurse -Force
New-Item $stage\source -ItemType Directory -Force
Set-Location $stage
New-Item "$stage\TestApplication" -ItemType Directory
Write-HostFormatted "Copyingg Bin" -Section
Copy-Item "$Sourcepath\Bin" "$stage\Bin" -Recurse -Force -Verbose
Write-HostFormatted "Copyingg TestWinApplication" -Section
Move-Item "$stage\Bin\TestWinApplication" "$stage\TestApplication" -Force -Verbose
Write-HostFormatted "Copyingg TestWebApplication" -Section
Move-Item "$stage\Bin\TestWebApplication" "$stage\TestApplication" -Force -Verbose
Write-HostFormatted "Copyingg AllTestsWin" -Section
Move-Item "$stage\Bin\AllTestWeb" "$stage\TestApplication" -Force -Verbose
Write-HostFormatted "Copyingg AllTestsWeb" -Section
Move-Item "$stage\Bin\AllTestWin" "$stage\TestApplication" -Force -Verbose
Remove-Item "$stage\bin\ReactiveLoggerClient" -Recurse -Force
Write-HostFormatted "Compressing TestApplication" -Section
Compress-Files "$stage\TestApplication" -zipfileName $sourcePath\Tests.zip -compressionLevel NoCompression
Write-HostFormatted "Compressing Bin" -Section
Compress-Files "$stage\Bin\" -zipfileName $sourcePath\bin.zip -compressionLevel NoCompression
Get-ChildItem $stage\bin|Remove-Item -Force -Recurse
Move-Item "$stage\bin.zip" "$stage\bin\bin.zip"
Get-ChildItem $stage\bin\TestApplication|Remove-Item -Force -Recurse
Move-Item "$stage\Tests.zip" "$stage\TestApplication\Tests.zip"