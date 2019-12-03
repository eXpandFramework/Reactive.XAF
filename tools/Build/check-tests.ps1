param(
    $Root="..\..\",
    $AzureToken=$env:AzDevopsToken,
    $DXApiFeed=$env:DXFeed,
    $BuildNumber="19.2",
    $DxPipelineBuildId
)
& $Root\go.ps1 -InstallModules
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
$latestPastVersion = $latestMinors | Select-Object -first 1 -skip 1
$firstPastVersion = $latestMinors | Select-Object -first 1 -skip 2
"latestPastVersion:$latestPastVersion"
if ($buildNumber -like "*$latestDxVersion*") {
    Add-AzBuildTag -tag "$latestdxVersion" 
    Write-Host "adding PublishNugets-DevExpress.XAF for build '$(DxPipelineBuildId)'"
    $parameters = @{
        DxPipelineBuildId = $DxPipelineBuildId
    }
    Add-AzBuild PublishNugets-DevExpress.XAF -Parameters $parameters
              
    $b = Get-AzBuilds -Tag "$latestPastVersion" -Status completed -Result succeeded -Top 1
    $b
    $parameters = @{
        DxPipelineBuildId = $b.id
    }
    Add-AzBuild -Definition DevExpress.XAF-Lab-Tests -Parameters $parameters
}
elseif ($BuildNumber -like "*$latestPastVersion*") {
    Add-AzBuildTag -tag "$latestPastVersion" 
    $b = Get-AzBuilds -Tag "$firstPastVersion" -Status completed -Result succeeded -Top 1
    $b
    $parameters = @{
        DxPipelineBuildId = $b.id
    }
    Add-AzBuild -Definition DevExpress.XAF-Lab-Tests -Parameters $parameters
}
else {
    Add-AzBuildTag -tag "$firstPastVersion" 
}