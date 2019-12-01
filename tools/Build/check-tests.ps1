param(
    $Token=(Get-AzureToken),
    $BuildName="DevExpress.XAF-Lab",
    $BuildNUmber="20191102.11"
)
Install-Module XpandPwsh -Scope CurrentUser -AllowClobber -Force
$testRunName="TestRun_$BuildName`_$BuildNumber"
"TestRunName=$testRunName"

$testRun=Invoke-AzureRestMethod $env:AzDevOpsToken eXpandDevOps eXpandFramework test/runs|Where-Object{$_.name -eq $testRunName}
$failedTests=$testRun.unanalyzedTests
if ($failedTests -gt 0){
    throw "$failedTests test fail"
}