param(
    [version]$CustomVersion=19.2.10.0
)

if ($CustomVersion -lt "20.2.0"){
    "DevExpress.ExpressApp.EasyTest.BlazorAdapter","DevExpress.ExpressApp.Blazor.All"|ForEach-Object{
        Remove-ContentLine (Get-PaketDependenciesPath) $_
    }
    
    Remove-ContentLine "$PSScriptRoot\..\src\Extensions\Xpand.Extensions.Office.Cloud.Google.Blazor\paket.references" "DevExpress.ExpressApp.Blazor"
}