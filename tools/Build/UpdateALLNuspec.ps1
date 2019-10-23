param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\")
)
Use-MonoCecil | Out-Null
function UpdateALLNuspec($platform,$allNuspec,$nuspecs) {
    $allNuspec.package.metadata.dependencies|Where-Object{
        $assembly
    }

    
    $allModuleNuspecs = $nuspecs | Where-Object { $_ -notlike "*ALL*" -and $_ -like "*.Modules.*" -and $_ -notlike "*.Client.*" }
    $platformNuspecs=$allModuleNuspecs | Where-Object {
        $platformMetada=Get-AssemblyMetadata "$root\bin\$($_.BaseName).dll"|Where-Object{$_.key -eq "Platform"}
        if (!$platformMetada){
            throw "Platform missing in $($_.baseName)"
        }
        $platformMetada.Value -eq $platform
    }| ForEach-Object {
        [xml]$nuspec = Get-Content $_.FullName
        [PSCustomObject]@{
            Nuspec = $nuspec
            File= $_
        }
    }
    $changed=$allNuspec.package.metadata.dependencies.dependency|Where-Object{

        $id=$_.Id
        $version=$_.Version
        if ($id -like "*.all"){
            [xml]$allCore=@(Get-Content "$root\tools\nuspec\Xpand.XAF.Core.All.nuspec")
        }
        !((($platformNuspecs|Select-Object -ExpandProperty Nuspec) + @($allCore))|Where-Object{
            $_.package.metaData.Id -eq $id -and $_.package.metaData.version -eq $version
        })
    }
    if ($changed){
        [version]$version=$allNuspec.package.metadata.version
        $allNuspec.package.metadata.version=(New-Object System.version($version.Major,$version.Minor,$version.Build,($version.Revision+1))).ToString()
    }
    if ($allNuspec.package.metadata.dependencies){
        $allNuspec.package.metadata.dependencies.RemoveAll()
    }
    
    $platformNuspecs| ForEach-Object {
        [xml]$nuspec = $_.Nuspec
        $dependency = [PSCustomObject]@{
            id      = $_.File.BaseName
            version = $nuspec.package.metadata.version
        }
        Add-NuspecDependency $dependency.Id $dependency.Version $allNuspec
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
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec "Core" $allNuspec $nuspecs
$allNuspec.Save($allFileName)
$coreDependency = [PSCustomObject]@{
    id      = $allNuspec.package.metadata.id
    version = $allNuspec.package.metadata.version
}

$allFileName = "$root\tools\nuspec\Xpand.XAF.Win.All.nuspec"
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec "Win" $allNuspec  $nuspecs
Add-NuspecDependency $coreDependency.Id $coreDependency.Version $allNuspec
$allNuspec.Save($allFileName)

$allFileName = "$root\tools\nuspec\Xpand.XAF.Web.All.nuspec"
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec "Web" $allNuspec  $nuspecs

Add-NuspecDependency $coreDependency.Id $coreDependency.Version $allNuspec
$allNuspec.Save($allFileName)