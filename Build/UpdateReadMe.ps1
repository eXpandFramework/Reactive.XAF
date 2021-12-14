using namespace system.text.RegularExpressions
param(
    $AzureToken =$env:AzureToken,
    [version]$ExistingVersion
)

$ErrorActionPreference = "Stop"
if ($buildNumber){
    $env:AzureToken=$AzureToken
    $env:AzOrganization="eXpandDevops"
    $env:AzProject ="eXpandFramework"
}

$rootLocation = "$PSScriptRoot\.."
Set-Location $rootLocation

$packagesPath = "$rootLocation\bin\Nupkg\"

if ($ExistingVersion){
    Clear-NugetCache XpandPackages
    Pop-XpandPackage Release -Version $ExistingVersion -PackageType XAFAll
    Get-ChildItem (Get-NugetInstallationFolder GlobalPackagesFolder) Xpand.XAF.Modules.*.Nupkg -Recurse|Copy-Item -Destination $packagesPath -Force
}
function GetPackages($packagesPath){
    (Get-XpandPackages -Source Lab -PackageType XAFModules).Id
}
$packages=GetPackages $packagesPath
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
function UpdateModulesList($rootLocation, $packages,$path) {
    $moduleList = "|PackageName|[![Custom badge](https://xpandshields.azurewebsites.net/endpoint.svg?label=Downloads&url=https%3A%2F%2Fxpandnugetstats.azurewebsites.net%2Fapi%2Ftotals%2FXAF)](https://www.nuget.org/packages?q=Xpand.XAF)<br>Platform/Target|About`r`n|---|---|---|`r`n"
    $projects=Get-MSBuildProjects $rootLocation\src\Modules
    $assemblies=(Get-ChildItem $rootLocation\bin -Recurse xpand.*.dll)+(Get-ChildItem $rootLocation\bin -Recurse xpand.*.exe)
    
    $packages | ForEach-Object {
        $name = $_.Replace("Xpand.XAF.Modules.", "")
        $packageUri = "[$name](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/$name)"
        $version = "![](https://xpandshields.azurewebsites.net/nuget/v/$_.svg?label=&style=flat)"
        $downloads = "![](https://xpandshields.azurewebsites.net/nuget/dt/$_.svg?label=&style=flat)"
        $_
        $targetFramework =$null
        if ($_ -like "*.All"){
            $regex = [regex] '(?is)Xpand\.XAF\.(?<platform>[^\.]*)\.All'
            $result = $regex.Match($_).Groups['platform'].Value;
            $platform="Agnostic"
            if ($result -ne "Core"){
                $platform=$result
            }
        }else{
            $packageName=$_
            $projectFile=$projects|Where-Object{$_.BaseName -eq $packageName}
            $project=$projectFile|Get-XmlContent
            $readMe="$($projectFile.DirectoryName)\Readme.md"
            $about=$null
            if (Test-Path $readMe){
                $readMeContent=Get-Content $readMe
                $regex = [regex] '(?is)# About(.*)## Details'
                $about = ($regex.Match($readMeContent).Groups[1].Value).trim();
                if (!$about){
                    throw "About for $packageName not found"
                }
            }
            
            if ($project){
                $targetFramework =Get-ProjectTargetFramework $project -FullName
            
                $assembly=$assemblies|Where-Object{$_.BaseName -eq $packageName}|Select-Object -First 1
                if ($assembly.Extension -eq ".exe"){
                    $platform="Win"
                }
                else{
                    $platform=($assembly|Get-AssemblyMetadata -key Platform).Value
                }
                if (!$platform){
                    throw "Platform missing in $packageName"
                }
                if ($platform -eq "Core"){
                    $platform="Agnostic"
                }
            }
        }
        
        $moduleList += "$packageUri|![](https://xpandshields.azurewebsites.net/badge/$platform-$targetFramework-yellowgreen)<br>$version$downloads|$about`r`n"
    }
    
    
    $allModulesReadMe = Get-Content $path -Raw
    
    $regex = [regex] '(?is)<moduleslist>(?<list>.*)</moduleslist>'
    $allModulesReadMe = $regex.Replace($allModulesReadMe, @"
<moduleslist>

$moduleList

</moduleslist>
"@
)
    Set-Content $path $allModulesReadMe.trim()
}

function UpdateDependencies($_, $packagespath, $readMePath) {
    $readMe = Get-Content $readMePath -Raw
    $metadata = ((Get-NugetPackageSearchMetadata -Name $_.BaseName -Source $packagesPath).DependencySets.Packages | ForEach-Object {
            $id = $_.Id
            if ($id -like "Xpand.XAF*") {
                $id = "[$id](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/$id)"
            }
            elseif ($id -eq "Fasterflect.Xpand") {
                $id = "[$id](https://github.com/eXpandFramework/Fasterflect)"
            }
            elseif ($id -eq "Xpand.VersionConverter") {
                $id = "[$id](https://github.com/eXpandFramework/Reactive.XAF/tree/master/tools/Xpand.VersionConverter)"
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

[![GitHub issues](https://xpandshields.azurewebsites.net/github/issues/eXpandFramework/expand/$package.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3A$package) [![GitHub close issues](https://xpandshields.azurewebsites.net/github/issues-closed/eXpandFramework/eXpand/$package.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3A$package)
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
To ``Step in the source code`` you need to ``enable Source Server support`` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/Reactive.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can ``unload`` it with the next call at the constructor of your module.
``````cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof($moduleName))
``````
$additionalTroubleShootingThe 
The [Reactive.Logger.Client.Win](https://github.com/eXpandFramework/Reactive.XAF/wiki/Reactive.Logger.Client.Win) is a must-have tool to help you troubleshoot and monitor in details a module, even from a remote location.
## Details
"@
    )


    Set-Content $readMePath $result.Trim()
}
function Tests($_, $packagespath, $readMePath) {
    $moduleName=(GetModuleName $_).Replace("Module","")
    $text=@"
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/$moduleName). 
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/Reactive.XAF#compatibility-matrix)
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
    UpdateModulesList $rootLocation $packages "$rootLocation\src\modules\ReadMe.md"
    UpdateModules $rootLocation $packages
}


    
