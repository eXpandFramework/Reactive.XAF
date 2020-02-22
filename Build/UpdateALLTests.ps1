param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    $branch = "lab",
    $source,
    $dxVersion = "19.2.6"
)
if ($branch -eq "lab" -and !$source) {
    $source = "$(Get-PackageFeed -Xpand);$(Get-PackageFeed -DX)"
}
if ($branch -eq "master") {
    $branch = "Release"
}
"dxVersion=$dxVersion"

$ErrorActionPreference = "Stop"
# Import-XpandPwsh
$excludeFilter = "*client*;*extension*"
$localPackages = & (Get-NugetPath) list -source "$root\bin\nupkg"|ConvertTo-PackageObject|Where-Object{$_.id -like "*.ALL"} | ForEach-Object {
    $version = [version]$_.Version
    if ($version.revision -eq 0) {
        $version = New-Object System.Version ($version.Major, $version.Minor, $version.build)
    }
    [PSCustomObject]@{
        Id      = $_.Id
        Version = $version
    }
}
Write-HostFormatted "LocalPackages:" -Section
$localPackages | Out-String
$remotePackages = Find-XpandPackage "Xpand*All*" -PackageSource Lab
Write-HostFormatted "remotePackages:" -Section
$remotePackages | Out-String
$latestPackages = (($localPackages + $remotePackages) | Group-Object Id | ForEach-Object {
        $_.group | Sort-Object Version -Descending | Select-Object -first 1
    })
Write-HostFormatted "latestPackages:" -Section
$latestPackages | Out-String
$packages = $latestPackages | Where-Object {
    $p = $_
    !($excludeFilter.Split(";") | Where-Object { $p.Id -like $_ })
}
Write-HostFormatted "finalPackages:" -Section
$packages | Out-String


$testApplication = "$root\src\Tests\ALL\TestApplication\TestApplication.sln"
Set-Location $root\src\Tests\All\
Write-HostFormatted "Update all package versions" -ForegroundColor Magenta
Get-ChildItem *.csproj -Recurse|ForEach-Object{
    $prefs=Get-PackageReference $_ 
    $prefs|Where-Object{$_.include -like "Xpand.XAF.*"}|ForEach-Object{
        $ref=$_
        $packages|Where-Object{$_.id-eq $ref.include}|ForEach-Object{
            $ref.version=$_.version.ToString()
        }
    }
    ($prefs|Select-Object -First 1).OwnerDocument.Save($_)
}

Write-HostFormatted "Building TestApplication" -Section
$testAppPAth = (Get-Item $testApplication).DirectoryName
Set-Location $testAppPAth
Clear-ProjectDirectories
New-Item "$root\bin\NupkgTemp" -ItemType Directory -Force
Write-HostFormatted "Create local package source" -ForegroundColor Magenta
$tempNupkg="$root\bin\NupkgTemp\"
Get-ChildItem "$root\bin\Nupkg"|Copy-Item -Destination $tempNupkg -Force
$psource="Release"
if ($branch -eq "lab"){
    $psource="Lab"
}
$tempPackages=(& (Get-NugetPath) list -source $tempNupkg|ConvertTo-PackageObject).id
Get-XpandPackages $psource All|Where-Object{$_.id -like "Xpand*"}|Where-Object{$_.id -notin $tempPackages}|Invoke-Parallel -VariablesToImport @("psource","tempNupkg") -script{
    Get-NugetPackage -name $_.id -Source (Get-PackageFeed $psource) -ResultType NupkgFile|Copy-Item -Destination $tempNupkg -Verbose
}

$localpackages=& (Get-NugetPath) list -Source "$root\bin\Nupkg\"|ConvertTo-PackageObject

Write-HostFormatted "Add binding redirects to TestApplication.Web" -ForegroundColor Magenta
Get-XpandPackages $branch XAFAll|ForEach-Object{
    $package=$_
    $localPackage=$localpackages|Where-Object{$_.id -eq $package.id}
    if ($localPackage){
        $localPackage
    }
    else{
        $package
    }
}|Where-Object{$_.id -notlike "*versionconverter*"}|ForEach-Object{
    Add-AssemblyBindingRedirect -id $_.id -version $_.version -path "$testAppPAth\TestApplication.web" -PublicToken (Get-XpandPublicKeyToken)
}
[xml]$config=Get-xmlContent "$testAppPAth\TestApplication.web\web.config"
$config.configuration.runtime.assemblyBinding.dependentassembly|ForEach-Object{
    [PSCustomObject]@{
        Name = $_.assemblyIdentity.Name
        Version=$_.bindingRedirect.NewVersion
    }
}|Format-Table
Invoke-Script {
    
    Write-HostFormatted "Complie TestApplication.sln" -ForegroundColor Magenta
    "alltestweb","alltestwin","Testwebapplication","testwinapplication"|ForEach-Object{
        if (Test-Path "$root\bin\$_"){
            Remove-Item "$root\bin\$_" -Force -Recurse
        }
    }
    & (Get-MsBuildPath) "$testAppPAth\TestApplication.sln" /t:Clean /v:m
    & (Get-NugetPath) restore "$testAppPAth\TestApplication.sln" -source "$tempNupkg;$(Get-PackageFeed -Nuget);$(Get-PackageFeed -Xpand)"
    & (Get-MsBuildPath) "$testAppPAth\TestApplication.sln" /bl:$root\bin\TestWebApplication.binlog /WarnAsError /v:m -t:rebuild -m
} -Maximum 2

Remove-Item $tempNupkg -Force -Recurse


