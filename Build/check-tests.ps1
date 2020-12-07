param(
    $Root = "..\",
    $AzureToken = $env:AzureToken,
    $DXApiFeed = $env:DXFeed,
    $DxPipelineBuildId
)
$ErrorActionPreference="stop"
& $Root\go.ps1 -InstallModules
Invoke-Script {
    $env:AzureToken = $AzureToken
    $env:AzOrganization = "eXpandDevOps"
    $env:AzProject = "eXpandFramework"
    $env:DXFeed = $DXApiFeed
    Write-Host "Checking for failed test"
    if (Get-AzTestRuns -buildIds $env:Build_BuildId -FailedOnly) {
        throw "There are fail tests"
    }
    if ($env:CustomVersion -eq (Get-XAFLatestMinors -Source $DXApiFeed|Select-Object -Last 1)){
        $parameters = @{
            CustomVersion = $env:CustomVersion
            DxPipelineBuildId = $env:DxPipelineBuildId
        }
        Add-AzBuild -Definition PublishNugets-DevExpress.XAF -Parameters $parameters
    }
    
}