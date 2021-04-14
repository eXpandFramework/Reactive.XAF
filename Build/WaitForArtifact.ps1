param(
    $BuildId = (Get-AzBuilds -Definition Reactive.XAF -Tag $CustomVersion -Status inProgress |Select-Object -First 1).Id,
    $GitHubToken=$env:GitHubToken,
    $GitUserEmail,
    $artifactName = "NugetConsumers",
    $CustomVersion=$env:CustomVersion
)

$env:AzProject="eXpandFramework"
$env:AzOrganization="eXpandDevOps"
& "$PSScriptRoot\..\go.ps1" -InstallModules -OnlyXpwsh
do {
    try {
        Write-HostFormatted "Checking artifact $artifactName in Build $BuildId"
        Start-Sleep -Seconds 5
        $a=Get-AzArtifact -BuildId $BuildId -ArtifactName $artifactName
    }
    catch {
        Write-Warning $_
    }
} until ($a)