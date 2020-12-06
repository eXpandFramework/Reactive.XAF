param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:DxFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken = $env:AzDevopsToken,
    [string]$CustomVersion = "20.2.4.0"
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
    . "$SourcePath\build\UpdateDependencies.ps1" $CustomVersion
    . "$SourcePath\build\UpdateLatestProjectVersion.ps1"
    
    

    $bArgs = @{
        packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
        tasklist       = $tasklist
        dxVersion      = $CustomVersion
        Branch         = $Branch
    }
    
    $SourcePath | ForEach-Object {
        Set-Location $_
        Move-PaketSource 0 $DXApiFeed
    }

    Set-Location "$SourcePath"
    

    Write-HostFormatted "Start-ProjectConverter version $CustomVersion"  -Section
    Start-XpandProjectConverter -version $CustomVersion -path $SourcePath -SkipInstall
    "PaketRestore $SourcePath"
    try {
        Invoke-PaketRestore -Strict 
    }
    catch {
        "PaketRestore Failed"
        Write-HostFormatted "PaketInstall $SourcePath (due to different Version)" -section
        dotnet paket install 
    }
    $nugetPackageFolder="$env:USERPROFILE\.nuget\packages"
    if (Test-AzDevops){
        $nugetPackageFolder="D:\a\1\.nuget\packages"
    }
    & powershell.exe "$SourcePath\build\targets\Xpand.XAF.Modules.JobScheduler.Hangfire.ps1" -nugetPackagesFolder $nugetPackageFolder
    
    Get-AssemblyPublicKeyToken (Get-ChildItem $nugetPackageFolder "*Hangfire.core.dll" -Recurse|Select-Object -First 1)
    # New-Command "Gac Assemblies" -commandPath "c:\windows\syswow64\WindowsPowerShell\v1.0\powershell.exe" -commandArguments "$SourcePath\build\targets\Xpand.XAF.Modules.JobScheduler.Hangfire.ps1"
    
    # if ($Branch -eq "lab") {
    #     Write-HostFormatted "checking for New DevExpress Version ($CustomVersion) " -Section
    #     $filter = "DevExpress*"
        
    #     [version]$currentVersion = Get-VersionPart (Get-DevExpressVersion) Build
    #     $outputFolder = "$([System.IO.Path]::GetTempPath())\GetNugetpackage"
    #     $rxdllpath=Get-ChildItem ((get-item (Get-NugetPackage -Name Xpand.XAF.Modules.Reactive -Source (Get-PackageFeed -Xpand) -OutputFolder $outputFolder -ResultType NupkgFile )).DirectoryName) "Xpand.XAF.Modules.Reactive.dll" -Recurse|Select-Object -First 1
    #     $assemblyReference=Get-AssemblyReference $rxdllpath.FullName
    #     [version]$publishdeVersion = Get-VersionPart (($assemblyReference | Where-Object { $_.Name -like $filter }).version) Build
    #     if ($publishdeVersion -lt $currentVersion) {
    #         Write-HostFormatted "new DX version detected $currentVersion"
    #         $trDeps = Get-NugetPackageDependencies DevExpress.ExpressApp.Core.all -Source $env:DxFeed -filter $filter -Recurse
    #         Push-Location 
    #         $projectPackages = (Get-ChildItem "$SourcePath\src\modules" *.csproj -Recurse) + (Get-ChildItem "$SourcePath\src\extensions" *.csproj -Recurse) | Invoke-Parallel -VariablesToImport "filter" -Script {
    #             Push-Location $_.DirectoryName
    #             [PSCustomObject]@{
    #                 Project           = $_
    #                 InstalledPackages = (Invoke-PaketShowInstalled -Project $_.FullName) | Where-Object { $_.id -like $filter }
    #             }
    #             Pop-Location
    #         }
    #         ($projectPackages | Where-Object { $_.InstalledPackages.id | Where-Object { $_ -in $trDeps.id } }).Project | Get-Item | ForEach-Object {
    #             Write-HostFormatted "Increase $($_.basename) revision" -ForegroundColor Magenta
    #             Update-AssemblyInfo $_.DirectoryName -Revision
    #         }
    #     }
    # }
    
    
    & $SourcePath\go.ps1 @bArgs

    Move-PaketSource 0 "C:\Program Files (x86)\DevExpress $(Get-VersionPart $DXVersion Minor)\Components\System\Components\Packages"
    New-Item  "$Sourcepath\Bin\Tests" -ItemType Directory -ErrorAction SilentlyContinue 
    Copy-Item "$Sourcepath\Bin" "$stage\Bin" -Recurse -Force 
    
}