using namespace system.text.RegularExpressions
param(
    $AzureToken =$env:AzDevOpsToken,
    [version]$ExistingVersion
)

$ErrorActionPreference = "Stop"
if ($buildNumber){
    $env:AzDevOpsToken=$AzureToken
    $env:AzOrganization="eXpandDevops"
    $env:AzProject ="eXpandFramework"
}

$rootLocation = "$PSScriptRoot\.."
Set-Location $rootLocation

$nuget = Get-NugetPath
$packagesPath = "$rootLocation\bin\Nupkg\"

if ($ExistingVersion){
    Clear-NugetCache XpandPackages
    Pop-XpandPackage Release -Version $ExistingVersion -PackageType XAFAll
    Get-ChildItem (Get-NugetInstallationFolder GlobalPackagesFolder) Xpand.XAF.Modules.*.Nupkg -Recurse|Copy-Item -Destination $packagesPath -Force
}
$packages=& $nuget List -Source $packagesPath | ConvertTo-PackageObject | Select-Object -ExpandProperty Id| Where-Object { $_ -notin @("Xpand.VersionConverter","Xpand.XAF.ModelEditor") -and $_ -notlike "Xpand.Extensions*" } 
function GetModuleName($_){
    $moduleName = $_.BaseName.Substring($_.BaseName.LastIndexOf(".") + 1)
    $moduleName = "$($_.BaseName).$($moduleName)Module"
    if ($moduleName -like "*.hub.*") {
        $moduleName = "Xpand.XAF.Modules.Reactive.Logger.Hub.ReactiveLoggerHubModule"
    }
    elseif ($moduleName -like "*Logger*") {
        $moduleName = "Xpand.XAF.Modules.Reactive.Logger.ReactiveLoggerModule"
    }
    $moduleName
}
function UpdateModulesList($rootLocation, $packages) {
    $moduleList = "|PackageName|Version|[![Custom badge](https://xpandshields.azurewebsites.net/endpoint.svg?label=Nuget.org&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAF)](https://www.nuget.org/packages?q=Xpand.XAF)`r`n|---|---|---|`r`n"
    $packages | ForEach-Object {
        $name = $_.Replace("Xpand.XAF.Modules.", "")
        $packageUri = "[$name](https://github.com/eXpandFramework/DevExpress.XAF/tree/master/src/Modules/$name)"
        $version = "![](https://xpandshields.azurewebsites.net/nuget/v/$_.svg?label=&style=flat)"
        $downloads = "![](https://xpandshields.azurewebsites.net/nuget/dt/$_.svg?label=&style=flat)"
        $moduleList += "$packageUri|$version|$downloads`r`n"
    }
    
    $path = "$rootLocation\src\modules\ReadMe.md"
    $allModulesReadMe = Get-Content $path -Raw
    
    $regex = [regex] '(?is)<twitter>(?<list>.*)</twitter>'
    $allModulesReadMe = $regex.Replace($allModulesReadMe, @"
<twitter>

$moduleList

</twitter>
"@
)
    Set-Content $path $allModulesReadMe.trim()
}

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
            if ($id -notlike "DevExpress*") {
                "|$id|$($_.VersionRange.MinVersion)`r`n"
            }
        })
    [xml]$csproj = Get-Content $_.FullName
    $dxDepends = (Get-PackageReference $_.FullName | Where-Object { $_.Include -like "DevExpress*" } | ForEach-Object {
            "|**$($_.Include -creplace '(.*)\.v[\d]{2}\.\d', '$1')**|**Any**`r`n"
        })
    $metadata = "|<!-- -->|<!-- -->`r`n|----|----`r`n$dxDepends$metadata"

    if ($readMe -notmatch "## Dependencies") {
        $readMe = $readMe.Replace("## Issues", "## Dependencies`r`n## Issues")
    }

    $version = $csproj.Project.PropertyGroup.TargetFramework|Where-Object{$_} | Select-Object -First 1
    $result = $readMe -creplace '## Dependencies([^#]*)', "## Dependencies`r`n``.NetFramework: $version```r`n`r`n$metadata`r`n"
    Set-Content $readMePath $result.Trim()
}
function UpdateBadges($_, $packagespath, $readMePath) {
    $readMe = Get-Content $readMePath -Raw
    $package = $_.BaseName.Replace("Xpand.XAF.Modules.", "")
    $badges = @"
![](https://xpandshields.azurewebsites.net/nuget/v/Xpand.XAF.Modules.$package.svg?&style=flat) ![](https://xpandshields.azurewebsites.net/nuget/dt/Xpand.XAF.Modules.$package.svg?&style=flat)

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/$package.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AStandalone_xaf_modules+$package) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/$package.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AStandalone_XAF_Modules+$package)
"@
    $readMe = [Regex]::replace($readMe, '(.*)# About', "$badges`r`n# About", [RegexOptions]::Singleline)
    Set-Content $readMePath $readMe.Trim()
}

function UpdateIssues($_, $packagespath, $readMePath) {
    $moduleName=& "$PSScriptRoot\ModuleName.ps1" $_
    $readMe = Get-Content $readMePath -Raw
    $regex = [regex] '(?isx)\#\#\ Issues(.*)\#\#\ Details'
    $result = $regex.Replace($readMe, @"
## Issues-Debugging-Troubleshooting
$1
To ``Step in the source code`` you need to ``enable Source Server support`` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can ``unload`` it with the next call at the constructor of your module.
``````cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof($moduleName))
``````
$additionalTroubleShootingThe 
The [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/DevExpress.XAF/wiki/Reactive.Logger.Client.Win) is a must-have tool to help you troubleshoot and monitor in details a module, even from a remote location.
## Details
"@
    )


    Set-Content $readMePath $result.Trim()
}
function Tests($_, $packagespath, $readMePath) {
    $moduleName=(GetModuleName $_).Replace("Module","")
    $text=@"
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/$moduleName). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)
"@
    $readMe=Get-Content $readMePath -raw
    $regex = [regex] '(?s)(### Tests(.*)### Examples)'
    $result = $regex.Replace($readMe, @"
### Tests
$text
### Examples
"@
)
    Set-Content $readMePath $result
}
function UpdateModules($rootLocation, $packages) {
    Get-ChildItem "$rootLocation\src" *.csproj -Recurse | Select-Object | ForEach-Object {
        $readMePath = "$($_.DirectoryName)\Readme.md"
        Write-Output "Updating $readMePath"
        if ((Test-Path $readMePath) -and $packages.Contains($_.BaseName)) {
            UpdateDependencies $_ $packagespath $readMePath
            UpdateBadges $_ $packagespath $readMePath
            UpdateIssues $_ $packagespath $readMePath
            Tests $_ $packagespath $readMePath
        }   
    }
}
if ($packages){
    UpdateModulesList $rootLocation $packages
    UpdateModules $rootLocation $packages
}


    
