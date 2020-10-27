param(
    [version]$CustomVersion=19.2.10.0
)

if ($CustomVersion -lt "20.1.8"){
    Remove-ContentLine (Get-PaketDependenciesPath) "DevExpress.ExpressApp.Blazor.All"
    Remove-ContentLine "$PSScriptRoot\..\src\Extensions\Xpand.Extensions.Office.Cloud.Google.Blazor\paket.references" "DevExpress.ExpressApp.Blazor"
}