param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:DxFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken = $env:AzureToken,
    [string]$CustomVersion = "21.1.5.0",
    [string]$UseLastVersion="1",
    $XpandBlobOwnerSecret=$env:AzXpandBlobOwnerSecret,
    $AzureApplicationId=$env:AzApplicationId,
    $AzureTenantId=$env:AzTenantId,
    [switch]$SkipVersioning
)

if (!(Get-Module eXpandFramework -ListAvailable)) {
    $env:AzureToken = $AzureToken
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
Invoke-Script {
    Set-VsoVariable build.updatebuildnumber "$env:build_BuildNumber-$CustomVersion"
    Set-Location $SourcePath
    try {
        dotnet nuget add source "https://api.nuget.org/v3/index.json" --name "nuget.org"
    }
    catch { }
    $LASTEXITCODE=0
    dotnet nuget list source
    Write-HostFormatted "Installing paket" -Section
    dotnet tool restore
}
Invoke-Script {
    
    $latestMinors = Get-XAFLatestMinors -Source "$DxApiFeed" 
    "latestMinors:"
    $latestMinors | Format-Table
    if ($latestMinors -is [array]){
        $CustomVersion = $latestMinors | Where-Object { "$($_.Major).$($_.Minor)" -eq $result }
    }
    else{
        $CustomVersion=$latestMinors
    }
    
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
    $bin = "$SourcePath\bin\"
    if (Test-Path $bin) {
        Get-ChildItem $bin -Recurse -Exclude "*Nupkg*" | Remove-Item -Force -Recurse
    }
    Set-Location "$SourcePath\src"
    Clear-XProjectDirectories
    Start-XpandProjectConverter -version $CustomVersion -path $SourcePath -SkipInstall
    "PaketRestore $SourcePath"
    try {
        Invoke-PaketRestore -Strict 
    }
    catch {
        Remove-Item "$SourcePath\bin" -Recurse -Force -ErrorAction SilentlyContinue
        "PaketRestore Failed"
        Write-HostFormatted "PaketInstall $SourcePath (due to different Version)" -section
        dotnet paket install 
    }
    $nugetPackageFolder = "$env:USERPROFILE\.nuget\packages"
    if (Test-AzDevops) {
        $nugetPackageFolder = "D:\a\1\.nuget\packages"
    }
    & powershell.exe "$SourcePath\build\targets\Xpand.XAF.Modules.JobScheduler.Hangfire.ps1" -nugetPackagesFolder $nugetPackageFolder
    
    Get-AssemblyPublicKeyToken (Get-ChildItem $nugetPackageFolder "*Hangfire.core.dll" -Recurse | Select-Object -First 1)

    & $SourcePath\go.ps1 @bArgs

    Move-PaketSource 0 "C:\Program Files (x86)\DevExpress $(Get-VersionPart $DXVersion Minor)\Components\System\Components\Packages"
    if (Test-AzDevops) {
        # Write-HostFormatted "Partition artifacts" -Section
        # "net461","net472"|ForEach-Object{
        #     Move-Item "$SourcePath\bin\$_" "$SourcePath\$_"
        # }
        
    }
    
}