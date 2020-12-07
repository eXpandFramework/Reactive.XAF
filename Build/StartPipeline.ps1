param(
    $Branch = "lab",
    $SourcePath = "$PSScriptRoot\..",
    $GitHubUserName = "apobekiaris",
    $GitHubToken = $env:GitHubToken,
    $DXApiFeed = $env:DxFeed,
    $artifactstagingdirectory,
    $bindirectory,
    [string]$AzureToken = $env:AzureToken,
    [string]$CustomVersion = "20.2.4.0",
    $UseLastVersion,
    $AzureApplicationId = $env:AzApplicationId,
    $AzureTenantId = $env:AzTenantId,
    $XpandBlobOwnerSecret = $env:AzXpandBlobOwnerSecret
)


if (!$UseLastVersion) {
    $goArgs = @{
        GithubToken              = $GitHubToken
        AzureToken               = $AzureToken
        GitHubUserName           = $GitHubUserName
        DXApiFeed                = $DXApiFeed
        Branch                   = $Branch
        SourcePath               = $SourcePath
        ArtifactStagingDirectory = $artifactstagingdirectory
        BinDirectory             = $bindirectory
        CustomVersion            = $CustomVersion
        AzStorageLookup          = $AzStorageLookup
        AzureApplicationId       = $AzureApplicationId
        AzureTenantId            = $AzureTenantId
        XpandBlobOwnerSecret     = $XpandBlobOwnerSecret
        UseLastVersion           = $UseLastVersion
    }
    & "$SourcePath\Build\BuildPipeline.ps1" @goArgs
}
else {
    Get-ChildItem $SourcePath
    # & "$SourcePath\go.ps1" -InstallModules
    # Connect-Az -ApplicationSecret $XpandBlobOwnerSecret -AzureApplicationId $AzureApplicationId -AzureTenantId $AzureTenantId
    # Write-HostFormatted  "Downloading into" -Section
    # $destination = "$SourcePath\bin\net461"
    # Get-AzStorageBlob -Container xpandbuild -Context (Get-AzStorageAccount | Where-Object { $_.StorageAccountName -eq "xpandbuildblob" }).Context | Get-AzStorageBlobContent -Destination $destination -Force | Out-Null
}

