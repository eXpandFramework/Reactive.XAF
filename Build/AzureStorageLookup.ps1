param(
    # $Branch = "lab",
    # $SourcePath = "$PSScriptRoot\..",
    # $GitHubUserName = "apobekiaris",
    # $GitHubToken = $env:GitHubToken,
    # $DXApiFeed = $env:DxFeed,
    # $artifactstagingdirectory,
    # $bindirectory,
    # [string]$AzureToken = $env:AzureToken,
    # [string]$CustomVersion = "20.2.4.0",
    # [bool]$UseLastVersion,
    # $AzureApplicationId = $env:AzApplicationId,
    # $AzureTenantId = $env:AzTenantId,
    # $XpandBlobOwnerSecret = $env:AzXpandBlobOwnerSecret
)


write-host "-----------------Installing Modules-----------------"
& "$PSScriptRoot\..\go.ps1" -InstallModules
Set-Location "$PSScriptRoot\.."
dotnet tool restore


New-Item -Name "AzStorage" -ItemType Directory -Path .
Write-HostFormatted  "Downloading from storage into AzStorage" -Section
$context=(Get-AzStorageAccount|Where-Object{$_.StorageAccountName -eq "xpandbuildblob"}).Context
Get-AzStorageBlob -Container xpandbuild -Context $context   | Get-AzStorageBlobContent -Destination "AzStorage" -Force | Out-Null
Get-ChildItem "AzStorage"|Copy-Item -Destination .\bin\net461 -Force -Verbose
Get-ChildItem .\bin\net461
Remove-Item "AzStorage" -Force -Recurse
Write-HostFormatted "Download Finished" -ForegroundColor Green