param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:LocalDXFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken = $env:AzDevopsToken,
    [string]$CustomVersion = "19.2.4"
)

if (!(Get-Module eXpandFramework -ListAvailable)) {
    $env:AzDevopsToken = $AzureToken
    $env:AzOrganization = "eXpandDevOps"
    $env:AzProject = "eXpandFramework"
    $env:DxFeed = $DxApiFeed
}
"CustomVersion=$CustomVersion"

$ErrorActionPreference = "Stop"
$regex = [regex] '(\d{2}\.\d*)'
$result = $regex.Match($CustomVersion).Groups[1].Value;
& "$SourcePath\go.ps1" -InstallModules

Set-VsoVariable build.updatebuildnumber "$env:build_BuildNumber-$CustomVersion"

if ($env:build_BuildId) {
    Add-AzBuildTag $CustomVersion
    if ($Branch -eq "master") {
        Add-AzBuildTag "Release"
    }
}

$stage = "$SourcePath\buildstage"
Remove-Item $stage -force -recurse -ErrorAction SilentlyContinue

Set-Location $SourcePath
dotnet tool restore

$latestMinors = Get-XAFLatestMinors
"latestMinors:"
$latestMinors
$CustomVersion = $latestMinors | Where-Object { "$($_.Major).$($_.Minor)" -eq $result }
"CustomVersion=$CustomVersion"

$DXVersion = Get-DevExpressVersion 

$taskList = "Build"
if ($Branch -eq "lab") {
    . "$SourcePath\tools\build\UpdateLatestProjectVersion.ps1"
}

$bArgs = @{
    packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist       = $tasklist
    dxVersion      = $CustomVersion
    ChangedModules = $updateVersion
    Debug          = !$Release
}
Write-HostFormatted "bArgs:" -Section
$bArgs | Out-String
$SourcePath | ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 $DXApiFeed
}

Set-Location "$SourcePath"
"PaketRestore $SourcePath"
# $paketConfigsPath="$SourcePath\.paket\.config\$newversion"
# New-Item $paketConfigsPath -ItemType Directory -Force
# Get-ChildItem "$paketConfigsPath\.."|where-Object{
#     $name=$_.Name 
#     !($latestMinors|Where-Object{$_ -eq $name})
# }|Remove-Item -Force -Recurse
# Get-ChildItem $paketConfigsPath|ForEach-Object{
#     Copy-Item $_.FullName $SourcePath -Force
# }
Write-HostFormatted "Start-ProjectConverter $CustomVersion"  -Section
Start-XpandProjectConverter -version $CustomVersion -path $SourcePath -SkipInstall

try {
    Invoke-PaketRestore -Strict 
    # Copy-Item $SourcePath\paket.lock $paketConfigsPath
    # Copy-Item $SourcePath\paket.dependencies $paketConfigsPath
}
catch {
    "PaketRestore Failed"
    Write-HostFormatted "PaketInstall $SourcePath (due to different Version)" -section
    dotnet paket install -v
    # Invoke-PaketInstall -Strict
    # Get-ChildItem $paketConfigsPath|Remove-Item 
    # Copy-Item $SourcePath\paket.lock $paketConfigsPath
    # Copy-Item $SourcePath\paket.dependencies $paketConfigsPath
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
Copy-Item "$Sourcepath\Bin" "$stage\Bin" -Recurse -Force 
Write-HostFormatted "Copyingg TestWinApplication" -Section
Move-Item "$stage\Bin\TestWinApplication" "$stage\TestApplication" -Force 
Write-HostFormatted "Copyingg TestWebApplication" -Section
Move-Item "$stage\Bin\TestWebApplication" "$stage\TestApplication" -Force 
Write-HostFormatted "Copyingg AllTestsWin" -Section
Move-Item "$stage\Bin\AllTestWeb" "$stage\TestApplication" -Force 
Write-HostFormatted "Copyingg AllTestsWeb" -Section
Move-Item "$stage\Bin\AllTestWin" "$stage\TestApplication" -Force 
Remove-Item "$stage\bin\ReactiveLoggerClient" -Recurse -Force

$DXVersion=Get-DevExpressVersion 
$SourcePath | ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 "C:\Program Files (x86)\DevExpress $DXVersion\Components\System\Components\Packages"
}