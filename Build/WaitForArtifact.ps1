param(
    $BuildId = (Get-AzBuilds -Definition DevExpress.XAF-Lab -Tag $CustomVersion -Status inProgress |Select-Object -First 1).Id,
    $GitHubToken=$env:GitHubToken,
    $GitUserEmail,
    $artifactName = "NugetConsumers",
    $CustomVersion=$env:CustomVersion
)

$env:AzProject="eXpandFramework"
$env:AzOrganization="eXpandDevOps"
& "$PSScriptRoot\..\go.ps1" -InstallModules -OnlyXpwsh
& "$PSScriptRoot\DownloadCloudCreds.ps1" -GitHubToken $GitHubToken -GitUserEmail $GitUserEmail
do {
    Write-HostFormatted "Checking artifact $artifactName in Build $BuildId"
    Start-Sleep -Seconds 5
} until (Get-AzArtifact -BuildId $BuildId -ArtifactName $artifactName)