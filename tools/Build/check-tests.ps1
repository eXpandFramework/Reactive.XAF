param(
    $Token=(Get-AzureToken),
    $BuildName,
    $BuildNUmber
)

$testRunName="TestRun_$BuildName`_$BuildNumber"
"TestRunName=$testRunName"
$testRun=Invoke-AzureRestMethod $Token eXpandDevOps eXpandFramework test/runs|Where-Object{$_.name -eq $testRunName}
$failedTests=$testRun.unanalyzedTests
if ($failedTests -gt 0){
    throw "$failedTests test fail"
}