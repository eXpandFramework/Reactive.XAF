param(
    $Root = "..\..\",
    $AzureToken = $env:AzDevopsToken,
    $DXApiFeed = $env:DXFeed,
    $BuildNumber = "19.2",
    $DxPipelineBuildId
)
$ErrorActionPreference="stop"
"DxPipelineBuildId=$DxPipelineBuildId"
"BuildNumber=$BuildNumber"
& $Root\go.ps1 -InstallModules
Invoke-Script {
    $env:AzDevopsToken = $AzureToken
    $env:AzOrganization = "eXpandDevOps"
    $env:AzProject = "eXpandFramework"
    $env:DXFeed = $DXApiFeed
    Write-Host "Checking for failed test"
    if (Get-AzTestRuns -buildIds $env:Build_BuildId -FailedOnly) {
        throw "There are fail tests"
    }
    Write-Host "Query latest minors"
    $latestMinors = Get-XAFLatestMinors
    $latestDxVersion = $latestMinors | Select-Object -first 1
    "latestDxVersion =$latestDxVersion" 
    $latestPastVersion = $latestMinors | Select-Object -first 1 -skip 1
    "latestPastVersion:$latestPastVersion"
    $firstPastVersion = $latestMinors | Select-Object -first 1 -skip 2
    "firstPastVersion=$firstPastVersion"
    if ($buildNumber -like "*$latestDxVersion*") {
        # Add-AzBuildTag -tag "$latestdxVersion" 
        Write-Host "adding PublishNugets-DevExpress.XAF for build $DxPipelineBuildId"
        $parameters = @{
            DxPipelineBuildId = $DxPipelineBuildId
        }
        Add-AzBuild PublishNugets-DevExpress.XAF -Parameters $parameters -Tag $latestDxVersion
              
        $b = Get-AzBuilds -Tag "$latestPastVersion" -Status completed -Result succeeded -Top 1 -Definition DevExpress.XAF-Lab
        $b
        $parameters = @{
            DxPipelineBuildId = $b.id
        }
        Add-AzBuild -Definition DevExpress.XAF-Lab-Tests -Parameters $parameters -Tag "$latestPastVersion"
    }
    elseif ($BuildNumber -like "*$latestPastVersion*") {
        # Add-AzBuildTag -tag "$latestPastVersion" 
        $b = Get-AzBuilds -Tag "$firstPastVersion" -Status completed -Result succeeded -Top 1 -Definition DevExpress.XAF-Lab
        $b
        $parameters = @{
            DxPipelineBuildId = $b.id
        }
        Add-AzBuild -Definition DevExpress.XAF-Lab-Tests -Parameters $parameters -Tag "$firstPastVersion"
    }
    else {
        # Add-AzBuildTag -tag "$firstPastVersion" 
    }
}