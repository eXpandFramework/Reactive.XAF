param(
    $Branch = "master",
    $nugetBin = "$PSScriptRoot\..\bin\Nupkg",
    $sourceDir = "$PSScriptRoot\..",
    $Filter ,
    [switch]$SkipReadMe,
    [string[]]$ChangedModules = @("Xpand.Extensions",
    "Xpand.Extensions.Mono.Cecil",
    "Xpand.Extensions.Reactive",
    "Xpand.Extensions.XAF",
    "Xpand.Extensions.XAF.Xpo",
    "Xpand.XAF.Modules.AutoCommit",
    "Xpand.XAF.Modules.CloneMemberValue",
    "Xpand.XAF.Modules.CloneModelView",
    "Xpand.XAF.Modules.GridListEditor",
    "Xpand.XAF.Modules.HideToolBar",
    "Xpand.XAF.Modules.MasterDetail",
    "Xpand.XAF.Modules.ModelMapper",
    "Xpand.XAF.Modules.ModelViewInheritance",
    "Xpand.XAF.Modules.OneView",
    "Xpand.XAF.Modules.ProgressBarViewItem",
    "Xpand.XAF.Modules.Reactive",
    "Xpand.XAF.Modules.Reactive.Logger",
    "Xpand.XAF.Modules.Reactive.Logger.Client.Win",
    "Xpand.XAF.Modules.Reactive.Logger.Hub",
    "Xpand.XAF.Modules.Reactive.Win",
    "Xpand.XAF.Modules.RefreshView",
    "Xpand.XAF.Modules.SuppressConfirmation",
    "Xpand.XAF.Modules.ViewEditMode",
    "Xpand.XAF.Core.All",
    "Xpand.XAF.Web.All",
    "Xpand.XAF.Win.All")
)
Import-Module XpandPwsh -Force -Prefix X
$ErrorActionPreference = "Stop"
get-variable ChangedModules|Out-variable
New-Item $nugetBin -ItemType Directory -Force | Out-Null
Get-ChildItem $nugetBin | Remove-Item -Force -Recurse
$toolPackages=@("Xpand.VersionConverter","Xpand.XAF.ModelEditor")
& "$PSScriptRoot\PackTools.ps1" $toolPackages $Branch

if (!$ChangedModules){
    $ChangedModules
    Write-HostFormatted "Skipping package creation as no package changed" -ForegroundColor Yellow
    return
}

Set-Location $sourceDir
$assemblyVersions = & "$sourceDir\build\AssemblyVersions.ps1" $sourceDir

# Get-ChildItem "$sourceDir\tools\nuspec" "Xpand*$filter*.nuspec" -Recurse | ForEach-Object {
$nuspecs = Get-ChildItem "$sourceDir\build\nuspec" "Xpand.*$filter*.nuspec" -Exclude "*Tests*" -Recurse

$nugetPath = (Get-XNugetPath)

$packScript = {
    $name = $_.FullName
    $basePath = "$sourceDir\bin"
    if ($name -like "*Client*") {
        $basePath += "\ReactiveLoggerClient"
    }
    
    $packageName = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $assemblyItem = $assemblyVersions | Where-Object { $_.name -eq $packageName }
    
    $version = $assemblyItem.Version
    if ($packageName -like "*All") {
        [xml]$coreNuspec = Get-Content "$sourceDir\build\nuspec\$packagename.nuspec"
        $version = $coreNuspec.package.metadata.Version
    }
 
    Invoke-Script {
        Write-Output "$nugetPath pack $name -OutputDirectory $($nugetBin) -Basepath $basePath -Version $version " #-f Blue
        & $nugetPath pack $name -OutputDirectory $nugetBin -Basepath $basePath -Version $version -NoPackageAnalysis
    }
    
}
$varsToImport = @("assemblyVersions", "SkipReadMe", "nugetPath", "sourceDir", "nugetBin", "SkipReadMe")
$nuspecs | Invoke-Parallel -VariablesToImport $varsToImport -Script $packScript
# $nuspecs | ForEach-Object { Invoke-Command $packScript -ArgumentList $_ }
function AddReadMe {
    param(
        $Package,
        $Directory,
        [System.IO.FileInfo[]]$assemblyList,
        $modules
    )
    if ($Package.Id -like "Xpand.XAF*") {
        $moduleName = (Get-XAFModule $Directory $assemblyList).Name
        $wikiName="Modules"
        if ($moduleName){
            $wikiName="$moduleName".Replace("Module","")
        }
        $registration="RequiredModuleTypes.Add(typeof($moduleName));"
        if ($package.Id -like "*all*"){
            $registration=($modules|Where-Object{$_.platform -eq "Core" -or $package.id -like "*$($_.platform)*"}|ForEach-Object{"RequiredModuleTypes.Add(typeof($($_.FullName)));"}) -join "`r`n                                                "
        }
        
        $message = @"


        
++++++++++++++++++++++++  ++++++++
++++++++++++++++++++++##  ++++++++      ➤ 𝗘𝗫𝗖𝗟𝗨𝗦𝗜𝗩𝗘 𝗦𝗘𝗥𝗩𝗜𝗖𝗘𝗦 @ 
++++++++++++++++++++++  ++++++++++          ☞ http://apobekiaris.expandframework.com
++++++++++    ++++++  ++++++++++++      
++++++++++++  ++++++  ++++++++++++      ➤  ɪғ ʏᴏᴜ ʟɪᴋᴇ ᴏᴜʀ ᴡᴏʀᴋ ᴘʟᴇᴀsᴇ ᴄᴏɴsɪᴅᴇʀ ᴛᴏ ɢɪᴠᴇ ᴜs ᴀ STAR. 
++++++++++++++  ++  ++++++++++++++          ☞ https://github.com/eXpandFramework/DevExpress.XAF/stargazers
++++++++++++++    ++++++++++++++++      
++++++++++++++  ++  ++++++++++++++      ➤ Package Notes
++++++++++++  ++++    ++++++++++++         ☞ Build the project before opening the model editor.
++++++++++  ++++++++  ++++++++++++         ☞ Documentation can be found @ https://github.com/eXpandFramework/DevExpress.XAF/wiki/$wikiName.
++++++++++  ++++++++++  ++++++++++         ☞ $($package.id) only adds the required references. To register the included packages add the next line/s in the constructor of your XAF module.
++++++++  ++++++++++++++++++++++++              $registration
++++++  ++++++++++++++++++++++++++      
        
"@
        Set-Content "$Directory\ReadMe.txt" $message
    }
    else {
        Remove-Item "$Directory\ReadMe.txt" -Force -ErrorAction SilentlyContinue
    }
}

Write-HostFormatted "Discover XAFModules" -Section
$packages=& (Get-NugetPath) list -source $nugetBin|ConvertTo-PackageObject|Where-Object{$_.id -notin $toolPackages}
$assemblyList=(Get-ChildItem "$nugetbin\.." *.dll)
$modules=Get-XAFModule "$nugetBin\.." -Include "Xpand.XAF.Modules.*" -AssemblyList $assemblyList|ForEach-Object{
    [PSCustomObject]@{
        FullName = $_.FullName
        platform=(Get-AssemblyMetadata $_.Assembly -key platform).value
    }
}

Write-HostFormatted "Update Nupkg files (ReadMe)" -Section
# $packages|Invoke-Parallel -VariablesToImport "nugetbin","modules","assemblylist" -LimitConcurrency ([System.Environment]::ProcessorCount) -Script{
$packages | ForEach-Object {
    $baseName="$($_.Id).$($_.Version)"
    $zip = "$nugetbin\$baseName.zip" 
    $nupkgPath="$nugetBin\$baseName.nupkg"
    Move-Item $nupkgPath $zip
    $unzipDir = "$nugetBin\$baseName"
    Expand-Archive $zip $unzipDir
    Remove-Item $zip
    AddReadme $_ $unzipDir $assemblyList $modules
    Compress-Files "$unzipDir" $zip 
    Move-Item $zip $nupkgPath
    Remove-Item $unzipDir -Force -Recurse
}
if ($Branch -ne "lab"){
    Write-HostFormatted "Update ReadMe" -Section
    & "$PSScriptRoot\UpdateReadMe.ps1" 
}
Write-HostFormatted "Remove not changed packages" -Section
if ($ChangedModules) {
    
    $core=@(Get-ChildItem "$sourceDir\bin" Xpand*.dll|Where-Object{$_.BaseName -in $ChangedModules }|ForEach-Object{(Get-AssemblyMetadata $_.FullName -Key platform).value}|Get-Unique|ForEach-Object{
        "Xpand.XAF.$_.All"
    })
    if ($core|Select-String "core"){
        $core+="Xpand.XAF.Win.All","Xpand.XAF.Web.All"
    }
    $core=$core|Sort-Object -Unique
    $ChangedModules+=$core
    $s="lab"
    if ($Branch -ne $s){
        $s="Release"
    }
    $toolPackages|ForEach-Object{
        if ((Find-XpandPackage $_ $s).Version -ne (Get-NugetPackageSearchMetadata $_ -Source $nugetBin).identity.version.version){
            $ChangedModules+=$_
        }
    }
    
    "ChangedModules:"
    $ChangedModules|Write-Output
    $nupks = Get-ChildItem $nugetBin
    & (Get-NugetPath) list -source $nugetBin | ConvertTo-PackageObject| ForEach-Object {
        $p = $_
        if ($p.Id -notin $ChangedModules) {
            $nupks | Where-Object { $_.BaseName -eq "$($p.Id).$($p.Version)" }
        }
    } | Remove-Item -Verbose
    
}
