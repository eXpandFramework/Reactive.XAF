param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    $branch = "lab",
    $source,
    $dxVersion = (& "$PSScriptRoot\DefaultVersion.ps1")
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

$localSource = "$root\bin\Nupkg"
$source = "$localSource;$(Get-PackageFeed -Nuget);$(Get-PackageFeed -Xpand);$source"
"Source=$source"
$testAppPAth = (Get-Item $testApplication).DirectoryName
Invoke-Script {
    & (Get-NugetPath) restore "$testAppPAth\TestApplication.sln" -source $source
    & (Get-MsBuildPath) "$testAppPAth\TestApplication.sln" /bl:$root\bin\TestWebApplication.binlog /WarnAsError /v:m -t:rebuild -m
} -Maximum 2


