param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:LocalDXFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken = $env:AzDevopsToken,
    [string]$CustomVersion = "19.2.4.0"
)

if (!(Get-Module eXpandFramework -ListAvailable)) {
    $env:AzDevopsToken = $AzureToken
    $env:AzOrganization = "eXpandDevOps"
    $env:AzProject = "eXpandFramework"
    $env:DxFeed = $DxApiFeed
}
"XpandPwsh"
Get-Module XpandPwsh -ListAvailable
"CustomVersion=$CustomVersion"

$ErrorActionPreference = "Stop"
$regex = [regex] '(\d{2}\.\d*)'
$result = $regex.Match($CustomVersion).Groups[1].Value;
& "$SourcePath\go.ps1" -InstallModules

Set-VsoVariable build.updatebuildnumber "$env:build_BuildNumber-$CustomVersion"
Pop-XafPackage

$stage = "$SourcePath\buildstage"
Remove-Item $stage -force -recurse -ErrorAction SilentlyContinue

Set-Location $SourcePath
dotnet tool restore

$latestMinors = Get-XAFLatestMinors
"latestMinors:"
$latestMinors|Format-Table
$CustomVersion = $latestMinors | Where-Object { "$($_.Major).$($_.Minor)" -eq $result }
"CustomVersion=$CustomVersion"

$DXVersion = Get-DevExpressVersion 

$taskList = "Build"
. "$SourcePath\build\UpdateLatestProjectVersion.ps1"

$bArgs = @{
    packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist       = $tasklist
    dxVersion      = $CustomVersion
    ChangedModules = @($updateVersion)
    Branch         = $Branch
}
Write-HostFormatted "bArgs:" -Section
$bArgs | Out-String
$SourcePath | ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 $DXApiFeed
}

Set-Location "$SourcePath"
"PaketRestore $SourcePath"

Write-HostFormatted "Start-ProjectConverter version $CustomVersion"  -Section
Start-XpandProjectConverter -version $CustomVersion -path $SourcePath -SkipInstall

try {
    Invoke-PaketRestore -Strict 
}
catch {
    "PaketRestore Failed"
    Write-HostFormatted "PaketInstall $SourcePath (due to different Version)" -section
    dotnet paket install -v
}
if ($Branch -eq "lab") {
    Write-HostFormatted "checking for New DevExpress Version ($CustomVersion) " -Section
    $filter = "DevExpress*"
    # [version]$currentVersion = (Invoke-PaketShowInstalled -OnlyDirect | Where-Object { $_.id -like $filter } | Select-Object -First 1).Version
    [version]$currentVersion = Get-VersionPart (Get-DevExpressVersion) Build
    $outputFolder = "$([System.IO.Path]::GetTempPath())\GetNugetpackage"
    $rxdllpath=Get-ChildItem ((get-item (Get-NugetPackage -Name Xpand.XAF.Modules.Reactive -Source (Get-PackageFeed -Xpand) -OutputFolder $outputFolder -ResultType NupkgFile )).DirectoryName) "Xpand.XAF.Modules.Reactive.dll" -Recurse|Select-Object -First 1
    $assemblyReference=Get-AssemblyReference $rxdllpath.FullName
    [version]$publishdeVersion = Get-VersionPart (($assemblyReference | Where-Object { $_.Name -like $filter }).version) Build
    if ($publishdeVersion -lt $currentVersion) {
        Write-HostFormatted "new DX version detected $currentVersion"
        $trDeps = Get-NugetPackageDependencies DevExpress.ExpressApp.Core.all -Source $env:DxFeed -filter $filter -Recurse
        Push-Location 
        $projectPackages = (Get-ChildItem "$SourcePath\src\modules" *.csproj -Recurse) + (Get-ChildItem "$SourcePath\src\extensions" *.csproj -Recurse) | Invoke-Parallel -VariablesToImport "filter" -Script {
            Push-Location $_.DirectoryName
            [PSCustomObject]@{
                Project           = $_
                InstalledPackages = (Invoke-PaketShowInstalled -Project $_.FullName) | Where-Object { $_.id -like $filter }
            }
            Pop-Location
        }
        ($projectPackages | Where-Object { $_.InstalledPackages.id | Where-Object { $_ -in $trDeps.id } }).Project | Get-Item | ForEach-Object {
            Write-HostFormatted "Increase $($_.basename) revision" -ForegroundColor Magenta
            Update-AssemblyInfo $_.DirectoryName -Revision
            $bArgs.ChangedModules += $_.basename
        }
    }
}

if ($bArgs.ChangedModules) {
    $bArgs.ChangedModules = $bArgs.ChangedModules | Sort-Object -Unique
}
Write-HostFormatted "ChangedModules" -Section
$bArgs.ChangedModules
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
Write-HostFormatted "Restore paket source" -ForegroundColor Magenta
$DXVersion = Get-VersionPart (Get-DevExpressVersion) Minor
$SourcePath | ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 "C:\Program Files (x86)\DevExpress $DXVersion\Components\System\Components\Packages"
}
Write-HostFormatted "FINISH" -Section