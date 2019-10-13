using namespace system.text.RegularExpressions
$ErrorActionPreference = "Stop"
$rootLocation = "$PSScriptRoot\..\.."
Set-Location $rootLocation

$nuget = Get-NugetPath
$packagesPath = "$rootLocation\bin\Nupkg\"
$packages = & $nuget List -Source $packagesPath | ConvertTo-PackageObject | Select-Object -ExpandProperty Id
function UpdateModulesList($rootLocation, $packages) {
    $moduleList = "|PackageName|Version|[![Custom badge](https://img.shields.io/endpoint.svg?label=Nuget.org&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAF)](https://www.nuget.org/packages?q=Xpand.XAF)`r`n|---|---|---|`r`n"
    $packages | Where-Object { $_ -ne "Xpand.VersionConverter" } | ForEach-Object {
        $name = $_.Replace("Xpand.XAF.Modules.", "")
        $packageUri = "[$name](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/$name)"
        $version = "![](https://img.shields.io/nuget/v/$_.svg?label=&style=flat)"
        $downloads = "![](https://img.shields.io/nuget/dt/$_.svg?label=&style=flat)"
        $moduleList += "$packageUri|$version|$downloads`r`n"
    }

    $path = "$rootLocation\src\modules\ReadMe.md"
    $allModulesReadMe = Get-Content $path -Raw
    $allModulesReadMe = [Regex]::replace($allModulesReadMe, "(## Platform agnostic modules list)(.*)(## Issues)", "`$1`r`n$moduleList`r`n`$3", [RegexOptions]::Singleline)
    set-content $path $allModulesReadMe.trim()
}
UpdateModulesList $rootLocation $packages
function UpdateDependencies($_, $packagespath, $readMePath) {
    $readMe = Get-Content $readMePath -Raw
    $metadata = ((Get-NugetPackageSearchMetadata -Name $_.BaseName -Source $packagesPath).DependencySets.Packages | ForEach-Object {
            $id = $_.Id
            if ($id -like "Xpand.XAF*") {
                $id = "[$id](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/$id)"
            }
            elseif ($id -eq "Xpand.VersionConverter") {
                $id = "[$id](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/tools/Xpand.VersionConverter)"
            }
            if ($id -notlike "DevExpress*"){
                "|$id|$($_.VersionRange.MinVersion)`r`n"
            }
        })
    [xml]$csproj = Get-Content $_.FullName
    $dxDepends = ($csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -like "DevExpress*" } | ForEach-Object {
            "|**$($_.Include -creplace '(.*)\.v[\d]{2}\.\d', '$1')**|**Any**`r`n"
        })
    $metadata = "|<!-- -->|<!-- -->`r`n|----|----`r`n$dxDepends$metadata"

    if ($readMe -notmatch "## Dependencies") {
        $readMe = $readMe.Replace("## Issues", "## Dependencies`r`n## Issues")
    }

    $version = $csproj.Project.PropertyGroup.TargetFrameworkVersion | Select-Object -First 1
    $result = $readMe -creplace '## Dependencies([^#]*)', "## Dependencies`r`n``.NetFramework: $version```r`n`r`n$metadata`r`n"
    Set-Content $readMePath $result.Trim()
}
function UpdateBadges($_, $packagespath,  $readMePath) {
    $readMe = Get-Content $readMePath -Raw
    $package = $_.BaseName.Replace("Xpand.XAF.Modules.","")
    $badges = @"
![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.$package.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.$package.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/$package.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+$package) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/$package.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+$package)
"@
    $readMe = [Regex]::replace($readMe, '(.*)# About', "$badges`r`n# About", [RegexOptions]::Singleline)
    Set-Content $readMePath $readMe.Trim()
}
function UpdateIssues($_, $packagespath,  $readMePath) {
    $moduleName=$_.BaseName.Substring($_.BaseName.LastIndexOf(".")+1)
    $moduleName="$($_.BaseName).$($moduleName)Module"
    if ($moduleName -like "*.hub.*"){
        $moduleName="Xpand.XAF.Modules.Reactive.Logger.Hub.ReactiveLoggerHubModule"
    }
    elseif ($moduleName -like "*Logger*"){
        $moduleName="Xpand.XAF.Modules.Reactive.Logger.ReactiveLoggerModule"
    }
    $readMe = Get-Content $readMePath -Raw
    $regex = [regex] '(?isx)\#\#\ Issues(.*)\#\#\ Details'
$result = $regex.Replace($readMe, @"
## Issues-Debugging-Troubleshooting
$1
To ``Step in the source code`` you need to ``enable Source Server support`` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can ``unload`` it with the next call at the contructor of your module.
``````cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof($moduleName))
``````
$additionalTroubleShooting
## Details
"@
)


    Set-Content $readMePath $result.Trim()
}
function UpdateModules($rootLocation, $packages) {
    Get-ChildItem "$rootLocation\src" *.csproj -Recurse | Select-Object | ForEach-Object {
        $readMePath = "$($_.DirectoryName)\Readme.md"
        if ((Test-path $readMePath) -and $packages.Contains($_.BaseName)) {
            UpdateDependencies $_ $packagespath $readMePath
            UpdateBadges $_ $packagespath $readMePath
            UpdateIssues $_ $packagespath $readMePath
            Tests $_ $packagespath $readMePath
        }   
    }
}

function Tests($_, $packagespath,  $readMePath) {

}
UpdateModules $rootLocation $packages
