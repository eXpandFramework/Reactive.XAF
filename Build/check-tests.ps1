param(
    $Root = "..\",
    $AzureToken = $env:AzureToken,
    $DXApiFeed = $env:DXFeed,
    $DxPipelineBuildId,
    [switch]$Publish
)
$ErrorActionPreference = "stop"
& $Root\go.ps1 -InstallModules
$VerbosePreference="continue"
Invoke-Script {
    $env:AzureToken = $AzureToken
    $env:AzOrganization = "eXpandDevOps"
    $env:AzProject = "eXpandFramework"
    $env:DXFeed = $DXApiFeed
    Write-Host "Checking for failed test"
    if (Get-AzTestRuns -buildIds $env:Build_BuildId -FailedOnly) {
        throw "There are fail tests in $DxPipelineBuildId"
    }
    if ($env:CustomVersion -eq (Get-XAFLatestMinors -Source $DXApiFeed | Select-Object -First 1) -and $Publish) {
        $failedBuilds = Get-XAFLatestMinors -Source $env:DxFeed|Where-Object{$_ -gt "20.1.0"} | ForEach-Object {
            $build = Get-AzBuilds -Definition Reactive.XAF-Lab-Tests -Tag "$_" -Top 1 -BranchName $env:Build_SourceBranchName
            Get-AzTestRuns -buildIds $build.Id -FailedOnly
        }
        "failedBuilds=$failedBuilds"
        "$failedBuilds".Length
        if (!("$failedBuilds".Trim())) {
            $parameters = @{
                CustomVersion     = $env:CustomVersion
                DxPipelineBuildId = $DxPipelineBuildId
            }
            "Parameters:"
            $parameters
            Add-AzBuild -Definition PublishNugets-Reactive.XAF -Parameters $parameters -Branch $env:Build_SourceBranchName
        }
    }
    
}