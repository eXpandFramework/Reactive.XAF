param(
    $Branch = "master",
    $SourcePath = "$PSScriptRoot\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:LocalDXFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken = $env:AzDevopsToken,
    [string]$CustomVersion = "20.1.4.0"
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
$todoTestsPath="$SourcePath\src\Tests\Office.Cloud.Microsoft.Todo\"
if (!(Test-Path $todoTestsPath\AzureAppCredentials.json) -or !(Get-Content $todoTestsPath\AzureAppCredentials.json -Raw)){
    Write-HostFormatted "Download office credential" -Section
    Remove-Item $env:TEMP\storage -Force -Recurse -ErrorAction SilentlyContinue
    Set-Location $env:TEMP
    git clone "https://apobekiaris:$GithubToken@github.com/eXpandFramework/storage.git"
    Set-Location $env:TEMP\storage\Azure
    "AzureAppCredentials.json","AuthenticationData.json"|Copy-Item -Destination $todoTestsPath -Force
}
Clear-NugetCache -Filter XpandPackages
Invoke-Script{
    Set-VsoVariable build.updatebuildnumber "$env:build_BuildNumber-$CustomVersion"
    $stage = "$SourcePath\buildstage"
    Remove-Item $stage -force -Recurse -ErrorAction SilentlyContinue
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
    Write-HostFormatted "ChangedModules:" -Section
    $bArgs.ChangedModules|Sort-Object | Out-String
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
    Write-HostFormatted "Copying Bin" -Section
    if (Test-AzDevops){
        Move-Item "$Sourcepath\Bin" "$stage\Bin" -Force 
    }
    else{
        Copy-Item "$Sourcepath\Bin" "$stage\Bin" -Recurse -Force 
    }
    
    Write-HostFormatted "Moving TestWinApplication" -Section
    Move-Item "$stage\Bin\TestWinApplication" "$stage\TestApplication" -Force 
    Write-HostFormatted "Moving TestWebApplication" -Section
    Move-Item "$stage\Bin\TestWebApplication" "$stage\TestApplication" -Force 
    Write-HostFormatted "Moving AllTestsWin" -Section
    Move-Item "$stage\Bin\AllTestWeb" "$stage\TestApplication" -Force 
    Write-HostFormatted "Moving AllTestsWeb" -Section
    Move-Item "$stage\Bin\AllTestWin" "$stage\TestApplication" -Force 
    Remove-Item "$stage\bin\ReactiveLoggerClient" -Recurse -Force
    
    Move-PaketSource 0 "C:\Program Files (x86)\DevExpress $(Get-VersionPart $DXVersion Minor)\Components\System\Components\Packages"

    "Web","Win"|ForEach-Object{
        Write-HostFormatted "Zipping DX $_" -ForegroundColor Magenta
        $webassemblies=((Get-ChildItem "$stage\TestApplication\AllTest$_" DevExpress*.dll -Recurse)+(Get-ChildItem ("$stage\TestApplication\Test$_","Application" -join "") DevExpress*.dll -Recurse))
        New-Item $stage\DX$_ -ItemType Directory -Force
        $webassemblies|Move-Item -Destination $stage\DX$_ -Force
        Compress-Files $stage\DX$_ $stage\DX$_.Zip -compressionLevel NoCompression 
        Remove-Item $stage\DX$_ -Force -Recurse
        Get-ChildItem "$stage\bin" DevExpress*.dll|Remove-Item
        New-Item $stage\DX -ItemType Directory -Force
        Move-Item $stage\DX$_.Zip $stage\DX
    }
    Write-HostFormatted "FINISH" -Section
}