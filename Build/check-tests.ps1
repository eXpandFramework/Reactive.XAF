param(
    $Root = "..\",
    $AzureToken = $env:AzureToken,
    $DXApiFeed = $env:DXFeed,
    $DxPipelineBuildId
)
$ErrorActionPreference = "stop"
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
    if ($env:CustomVersion -eq (Get-XAFLatestMinors -Source $DXApiFeed | Select-Object -Last 1)) {
        $failedBuilds = Get-XAFLatestMinors -Source $env:DxFeed|Where-Object{$_ -gt "20.1.0"} | ForEach-Object {
            $build = Get-AzBuilds -Definition Reactive.XAF-Lab-Tests -Tag "$_" -Top 1 -BranchName $env:Build_SourceBranchName
            Get-AzTestRuns -buildIds $build.Id -FailedOnly
        }
        "failedBuilds=$failedBuilds"
        if (!$failedBuilds) {
            $parameters = @{
                CustomVersion     = $env:CustomVersion
                DxPipelineBuildId = $env:DxPipelineBuildId
            }
            Add-AzBuild -Definition PublishNugets-Reactive.XAF -Parameters $parameters -Branch $env:Build_SourceBranchName
        }
    }
    
}