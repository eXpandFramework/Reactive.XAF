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
$localPackages = Get-ChildItem "$root\tools\nuspec" *ALL.nuspec | ForEach-Object {
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
$remotePackages = Get-XpandPackages -PackageType XAF -Source $branch | Where-Object { $_.Name -like "*All" }
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


$testApplication = "$root\src\Tests\ALL\TestApplication\TestApplication.sln"
$global:versionChanged=$false
function UpdateDXVersion( $dxVersion, $testApplication ) {
    $testAppDir = (Get-Item $testApplication).DirectoryName
    $shortDxVersion = Get-DevExpressVersion $dxVersion
    if (($dxVersion.ToCharArray() | Where-Object { $_ -eq "." }).Count -eq 2) {
        $dxVersion += ".0"
    }
    Get-ChildItem $testAppDir -Include "*.aspx", "*.config" -Recurse | ForEach-Object {
        $xml = Get-Content $_.FullName -Raw
        $regex = [regex] '(?<name>DevExpress.*)v\d{2}\.\d{1,2}(.*)Version=([.\d]*)'
        $result = $regex.Replace($xml, "`${name}v$shortDxVersion`$1Version=$dxversion")
        if ($result.Trim() -ne $xml.Trim()){
            $global:versionChanged=$true;
        }
        Set-Content $_.FullName $result.Trim()
    }
    
}
Get-ChildItem "$root\src\Tests\All\" *.csproj -Recurse | ForEach-Object {
    UpdateDXVersion $dxVersion $testApplication
}
Set-Location $root\src\Tests\All\
$depsFile=Get-PaketDependenciesPath
$depsRaw=Get-Content $depsFile -Raw
"Win","Web"|ForEach-Object{
    $regex = [regex] "(nuget Xpand\.XAF\.$_[^ ]*) (?<version>(\d\.)*\d)"
    $platform=$_
    $version=($packages|Where-Object{$_.Id -match $platform}).Version
    $result = $regex.Replace($depsRaw, "`$1 $version")
    if ($result.Trim() -ne $depsRaw.Trim()){
        $global:versionChanged=$true;
    }
    $depsRaw=$result
    Set-Content $depsFile $result.Trim()
    Write-Host ----$depsFile
    Write-Host $result
}


Set-Location "$root\src\Tests\All"
Invoke-Script {
    if ($global:versionChanged ){
        Write-Host "Paket Update" -f Green
        Invoke-PaketUpdate
    }
    else{
        Write-Host "Paket Restore" -f Green
        Invoke-PaketRestore
    }
}

Write-Host "Building TestApplication" -f Green

$localSource = "$root\bin\Nupkg"
$source = "$localSource;$(Get-PackageFeed -Nuget);$(Get-PackageFeed -Xpand);$source"
"Source=$source"
$testAppPAth = (Get-Item $testApplication).DirectoryName
# Invoke-Script {
#     & (Get-NugetPath) restore "$testAppPAth\TestApplication.Win\TestApplication.Win.csproj" -source $source
#     & (Get-MsBuildPath) "$testAppPAth\TestApplication.Win\TestApplication.Win.csproj" /bl:$root\bin\TestWebApplication.binlog /WarnAsError /v:m -t:rebuild
# }
Invoke-Script {
    & (Get-NugetPath) restore "$testAppPAth\TestApplication.sln" -source $source
    & (Get-MsBuildPath) "$testAppPAth\TestApplication.sln" /bl:$root\bin\TestWebApplication.binlog /WarnAsError /v:m -t:rebuild -m
}


