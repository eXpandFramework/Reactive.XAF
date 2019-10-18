param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    $branch="lab",
    $source
)
if ($branch -eq "lab" -and !$source){
    $source=Get-PackageFeed -Xpand
}
if ($branch -eq "master") {
    $branch = "Release"
}
$ErrorActionPreference = "Stop"
# Import-XpandPwsh
$excludeFilter = "*client*;*extension*"
$localPackages = Get-ChildItem "$root\tools\nuspec" *.nuspec | ForEach-Object {
    [xml]$nuspec = Get-Content $_.FullName
    $version = [version]$nuspec.package.metadata.Version
    if ($version.revision -eq 0) {
        $version = New-Object System.Version ($version.Major, $version.Minor, $version.build)
    }
    [PSCustomObject]@{
        Id      = $nuspec.package.metadata.id
        Version = $version
    }
}
Write-Host "LocalPackages:" -f blue
$localPackages | Out-String
$remotePackages = Get-XpandPackages -PackageType XAF -Source $branch
Write-Host "remotePackages:" -f Blue
$remotePackages | Out-String
$latestPackages = (($localPackages + $remotePackages) | Group-Object Id | ForEach-Object {
        $_.group | Sort-Object Version -Descending | Select-Object -first 1
    })
Write-Host "latestPackages:" -f Blue
$latestPackages | Out-String
$packages = $latestPackages | Where-Object {
    $p = $_
    !($excludeFilter.Split(";") | Where-Object { $p.Id -like $_ })
}
Write-Host "finalPackages:" -f Blue
$packages | Out-String

$csprojPath = "$root\src\Tests\All\Win\ALL.Win.Tests.csproj"
[xml]$csproj = Get-Content $csprojPath
function CheckForNotInstalled($csproj, $filter, $packages) {
    $ref = $csproj.project.itemgroup.PackageReference.Include | Where-Object { $_ -like "Xpand.*" }
    $packages | Where-Object { !$filter -or $_.Id -like "*.$filter" } | ForEach-Object {
        $id = $_.Id
        $installed = $ref | Select-String $id
        if (!$installed) {
            throw "$id $($_.Version) not installed in ALL$Filter.Tests" 
        }
    }
}
CheckForNotInstalled $csproj ".Win" $packages
CheckForNotInstalled $csproj "" $packages

$csproj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -like "Xpand.*" } | ForEach-Object {
    $pref = $_
    $packages | Where-Object { $_.Id -eq $pref.Include } | ForEach-Object {
        if ($_.Version -ne ([version]$pref.Version)) {
            Write-Host "$($_.Id) version changed from $($pref.Version) to $($_.Version)" -f Green
            $pref.Version = $_.Version.ToString()
        }
        
    }
}
$csproj.Save($csprojPath)
Get-Content $csprojPath -Raw
Write-Host "Building ALLTests" -f Blue
Get-ChildItem "$root\src\Tests\All" *.sln -Recurse | ForEach-Object {
    dotnet restore $_.FullName --source "$source;$root\bin\Nupkg" /WarnAsError
    dotnet msbuild $_.FullName "/p:configuration=Debug" /WarnAsError
    $allTestsBin="$root\bin\All\Win"
    New-Item $allTestsBin -ItemType Directory -Force -ErrorAction SilentlyContinue
    Get-ChildItem "$root\src\Tests\ALL\Win\bin\Debug" *.*|Copy-Item -Destination $allTestsBin
}