param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..\..",
    $GitHubUserName = "apobekiaris",
    $Pass = $env:GithubPass,
    $DXApiFeed  ,
    $artifactstagingdirectory,
    $AzureToken  =(Get-AzureToken),
    $WhatIf = $false
)
$ErrorActionPreference = "Stop"

& "$SourcePath\go.ps1" -InstallModules
$packageSource = Get-XPackageFeed -Xpand
if (!(Get-Module VSTeam -ListAvailable)) {
    Install-Module VSTeam -Force
}
Set-VSTeamAccount -Account eXpandDevOps -PersonalAccessToken $AzureToken

$localPackages = Get-ChildItem "$sourcePath\src\Modules" "*.csproj" -Recurse | Invoke-Parallel -VariablesToImport "Branch" -Script {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $localVersion = Get-XpandVersion -XpandPath $_.DirectoryName -module $name
    $nextVersion = Get-XpandVersion -Next -module $name
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
$publishedPackages = & (Get-XNugetPath) list Xpand.XAF.Modules -source $packageSource | ConvertTo-PackageObject | Where-Object { $_.Id -like "Xpand.XAF*" } | ForEach-Object {
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
Write-host "newPackages:" -f blue
$newPackages




$labBuild = Get-VSTeamBuild -ResultFilter succeeded -ProjectName expandframework -top 1 -StatusFilter completed -Definitions 23

$yArgs = @{
    Owner        = $GitHubUserName
    Organization = "eXpandFramework"
    Repository   = "DevExpress.XAF"
    Branch       = $Branch
    Pass         = $Pass
    Packages     = $publishedPackages 
    SourcePath   = $SourcePath
    CommitsSince = $labBuild.finishTime
    ExcludeFilter = "Test"
    WhatIf       = $WhatIf
}
if ($newPackages) {
    $yArgs.Packages += $newPackages
}
Write-Host "End-Packages:" -f blue
$yArgs.Packages 
Update-NugetProjectVersion @yArgs -Verbose

$bArgs = @{
    packageSources = "$(Get-PackageFeed -Xpand);$DxApiFeed"
    tasklist       = "release"
}

& $SourcePath\go.ps1 @bArgs

"$SourcePath\Bin\Nupkg", "$SourcePath\Bin\Nuspec" | ForEach-Object {
    Get-ChildItem $_ -Recurse | ForEach-Object {
        Copy-Item $_.FullName -Destination $artifactstagingdirectory
    }
}
