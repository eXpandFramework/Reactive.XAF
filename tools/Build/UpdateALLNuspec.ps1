param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    $Release=$true,
    $Branch="lab"
)
Use-MonoCecil | Out-Null
function UpdateALLNuspec($platform, $allNuspec, $nuspecs,$allModuleNuspecs) {
    
    $platformNuspecs = $allModuleNuspecs | Where-Object {
        $platformMetada = Get-AssemblyMetadata "$root\bin\$($_.BaseName).dll" -key "Platform"
        $platformMetada.Value -in $platform
    } | ForEach-Object {
        [xml]$nuspec = Get-Content $_.FullName
        [PSCustomObject]@{
            Nuspec = $nuspec
            File   = $_
        }
    }
    
    [version]$modulesVersion=Get-VersionPart ([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$root\bin\Xpand.XAF.Modules.Reactive.dll" ).FileVersion) build
    "Year Version build=$modulesVersion"
    $source="lab"
    if ($branch -eq "master"){
        $source="Release"
    }
    [version]$nowVersion = (Find-XpandPackage $allNuspec.package.metadata.Id -packagesource $source).Version
    $releasedVersion=[version](Get-VersionPart (Find-XpandPackage $allNuspec.package.metadata.Id -packagesource Release).Version Build)
    "release build=$releasedVersion"
    $nowBuild=([version](Get-VersionPart $nowVersion Build))
    "nowBuild=$nowBuild"
    if ($releasedVersion -gt $nowBuild){
        $nowVersion=[version]"$releasedVersion.0"
    }
    "nowVersion=$nowVersion"

    $build=$nowVersion.Build
    $revision=($nowVersion.Revision + 1)
    if ($Branch -eq "master"){
        $build+=1
        $revision=0
    }
    if ($nowVersion.Minor -ne $modulesVersion.Minor){
        $revision=0
    }
    $v = New-Object System.version($modulesVersion.Major, $modulesVersion.Minor, $build, $revision)
    "newVersion=$v"
    $allNuspec.package.metadata.version = ($v).ToString()
    if ($allNuspec.package.metadata.dependencies) {
        $allNuspec.package.metadata.dependencies.RemoveAll()
    }
    
    $platformNuspecs | ForEach-Object {
        [xml]$nuspec = $_.Nuspec
        $dependency = [PSCustomObject]@{
            id      = $_.File.BaseName
            version = $nuspec.package.metadata.version
        }
        
        Add-NuspecDependency $dependency.Id $dependency.version $allNuspec
    }
}
$nuspecs = Get-ChildItem "$root\tools\nuspec" *.nuspec
$nuspecs | ForEach-Object {
    [xml]$nuspec = Get-Content $_.FullName
    $nuspec.package.metaData.dependencies.dependency | Where-Object { $_.Id -like "DevExpress*" } | ForEach-Object {
        $_.ParentNode.RemoveChild($_)
    }
    $nuspec.Save($_.FullName)
}
$allFileName = "$root\tools\nuspec\Xpand.XAF.Core.All.nuspec"
Write-HostFormatted "Updating Xpand.XAF.Core.All.nuspec" -Section
[xml]$allNuspec = Get-Content $allFileName
$allModuleNuspecs = $nuspecs | Where-Object { $_ -notlike "*ALL*" -and ($_ -like "*.Modules.*" -or $_ -like "*.Extensions.*") -and $_ -notlike "*.Client.*" }
UpdateALLNuspec "Core" $allNuspec $nuspecs $allModuleNuspecs 
$allNuspec.Save($allFileName)
Get-Content $allFileName -Raw
$coreDependency = [PSCustomObject]@{
    id      = $allNuspec.package.metadata.id
    version = $allNuspec.package.metadata.version
}

$allFileName = "$root\tools\nuspec\Xpand.XAF.Win.All.nuspec"
Write-HostFormatted "Updating Xpand.XAF.Win.All.nuspec" -Section
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec @("Core","Win") $allNuspec  $nuspecs $allModuleNuspecs
Add-NuspecDependency $coreDependency.Id $coreDependency.Version $allNuspec
$coredependency=$allNuspec.package.metadata.dependencies.dependency|Where-Object{$_.id -eq "Xpand.XAF.Core.all"}|Select-Object -First 1
# $coreDependency.ParentNode.RemoveChild($coreDependency)
$allNuspec.Save($allFileName)
Get-Content $allFileName -Raw

$allFileName = "$root\tools\nuspec\Xpand.XAF.Web.All.nuspec"
Write-HostFormatted "Updating Xpand.XAF.Web.All.nuspec"
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec @("Core","Web") $allNuspec  $nuspecs $allModuleNuspecs 

Add-NuspecDependency $coreDependency.Id $coreDependency.Version $allNuspec
$allNuspec.Save($allFileName)
Get-Content $allFileName -Raw