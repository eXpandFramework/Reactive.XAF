param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\"),
    $Release=$false,
    $Branch="lab",
    $dxVersion
)
Use-MonoCecil | Out-Null
# $VerbosePreference ="continue"
function UpdateALLNuspec($platform, $allNuspec, $nuspecs,$allModuleNuspecs,$csProjects) {
    
    $platformNuspecs = $allModuleNuspecs | ForEach-Object {
        [xml]$nuspec = Get-Content $_.FullName
        
        if ($_.BaseName -eq "Xpand.XAF.Modules.Reactive"){
            Write-Verbose $_.BaseName
        }
        $nuspecBaseName=$_.BaseName
        $filesrc=($nuspec.package.Files.file|Where-Object{$_.src -like "*$nuspecBaseName.dll"}).src
        $platformMetada = Get-AssemblyMetadata "$root\bin\$filesrc" -key "Platform"
        if ($platformMetada.Value -in $platform){
            $target=Get-ProjectTargetFramework (Get-XmlContent ($csProjects|Where-Object{$_.BaseName -eq $nuspecBaseName }).FullName) -FullName
            [PSCustomObject]@{
                Nuspec = $nuspec
                File   = $_
                Target = $target
            }
        }        
        
    }
    
    $v=Get-AssemblyInfoVersion "$root\src\Common\AssemblyInfoVersion.cs"
    $allNuspec.package.metadata.version = ($v).ToString()
    if ($allNuspec.package.metadata.dependencies) {
        $allNuspec.package.metadata.dependencies.RemoveAll()
    }
    
    $platformNuspecs|Group-Object Target | ForEach-Object {
        $key=$_.Name
        
        $_.group|ForEach-Object{
            [xml]$nuspec = $_.Nuspec
            $dependency = [PSCustomObject]@{
                id      = $_.File.BaseName
                version = $nuspec.package.metadata.version
            }
            
            Add-NuspecDependency $dependency.Id $dependency.version $allNuspec $key
        }
        
    }
    $group=$allNuspec.package.metadata.dependencies.group
    $standardGroup=($group|Where-Object{$_.targetFramework -eq "netstandard2.0"})
    $net461Group=($group|Where-Object{$_.targetFramework -eq "net461"})
    if ($net461Group){
        $standardGroup.dependency|ForEach-Object{
            Add-NuspecDependency $_.Id $_.version $allNuspec "net461"
        }
    }
}

$nuspecs = Get-ChildItem "$root\build\nuspec" *.nuspec|Where-Object{$dxVersion -gt "20.2.0" -or $_.BaseName -notmatch "blazor|hangfire"}
$nuspecs | ForEach-Object {
    [xml]$nuspec = Get-Content $_.FullName
    $nuspec.package.metaData.dependencies.dependency | Where-Object { $_.Id -like "DevExpress*" } | ForEach-Object {
        $_.ParentNode.RemoveChild($_)
    }
    $nuspec.Save($_.FullName)
}
$allFileName = "$root\build\nuspec\Xpand.XAF.Core.All.nuspec"
Write-HostFormatted "Updating Xpand.XAF.Core.All.nuspec" -Section
[xml]$allNuspec = Get-Content $allFileName
$allModuleNuspecs = $nuspecs | Where-Object { $_ -notlike "*ALL*" -and ($_ -like "*.Modules.*" -or $_ -like "*.Extensions.*") -and $_ -notlike "*.Client.*" }
$csProjects=Get-MSBuildProjects $root\src 
UpdateALLNuspec "Core" $allNuspec $nuspecs $allModuleNuspecs $csProjects
$googleBlazorVersion=(Get-XmlContent "$root\Build\nuspec\Xpand.Extensions.Office.Cloud.Google.Blazor.nuspec").package.metadata.version
Add-NuspecDependency Xpand.Extensions.Office.Cloud.Google.Blazor $googleBlazorVersion $allNuspec "netstandard2.0"
$allNuspec|Save-Xml $allFileName

Get-Content $allFileName -Raw
$allFileName = "$root\build\nuspec\Xpand.XAF.Win.All.nuspec"
Write-HostFormatted "Updating Xpand.XAF.Win.All.nuspec" -Section
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec @("Core","Win") $allNuspec  $nuspecs $allModuleNuspecs $csProjects
$allNuspec|Save-Xml $allFileName
Get-Content $allFileName -Raw

$allFileName = "$root\build\nuspec\Xpand.XAF.Web.All.nuspec"
Write-HostFormatted "Updating Xpand.XAF.Web.All.nuspec"
[xml]$allNuspec = Get-Content $allFileName
UpdateALLNuspec @("Core","Web") $allNuspec  $nuspecs $allModuleNuspecs $csProjects

$allNuspec|Save-Xml $allFileName
Get-Content $allFileName -Raw