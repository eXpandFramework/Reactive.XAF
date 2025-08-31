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
    [switch]$SkipVersioning,
    $DXLicense
)

if ($DXLicense){
    $env:DevExpress_License=$DXLicense
    $licensePath = "$env:APPDATA\DevExpress\DevExpress_License.txt"
    Write-Host "Starting DevExpress license setup"
    $dir = Split-Path $licensePath
    Write-Host "Ensuring directory exists: $dir"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Write-Host "Writing license file to: $licensePath"
    Set-Content -Path $licensePath -Value $env:DXLicense -Encoding UTF8
    if (Test-Path $licensePath) {
        Write-Host "License file successfully written."
    } else {
        Write-Error "License file not found at $licensePath after write."
    }

}


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
    
    Set-Location $SourcePath
    try {
        dotnet nuget add source "https://api.nuget.org/v3/index.json" --name "nuget.org"
        dotnet nuget add source "$DXApiFeed" --name "DX"
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
    

    & $SourcePath\go.ps1 @bArgs

    Move-PaketSource 0 "c:\DevExpressPackages"


    if (Test-AzDevops) {
        # Write-HostFormatted "Partition artifacts" -Section
        # "net461","net472"|ForEach-Object{
        #     Move-Item "$SourcePath\bin\$_" "$SourcePath\$_"
        # }
        
    }
    
}