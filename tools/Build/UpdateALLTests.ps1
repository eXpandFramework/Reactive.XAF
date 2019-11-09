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
function UpdateVersion($csprojPath, $dxVersion, $testApplication ) {
    [xml]$csproj = Get-Content $csprojPath
    Write-Host "Update All package version $csprojPath"
    
    $packageReferences = Get-PackageReference $csprojPath
    $packageReferences | Where-Object { $_.Include -like "Xpand.*" } | ForEach-Object {
        $pref = $_
        $packages | Where-Object { $_.Id -eq $pref.Include } | ForEach-Object {
            if ($_.Version -ne ([version]$pref.Version)) {
                $global:versionChanged=$true
                Write-Host "$($_.Id) version changed from $($pref.Version) to $($_.Version)" -f Green
                $pref.Version = $_.Version.ToString()
                Invoke-PaketAdd -id $_.Id -version $_.Version -projectpath $csprojPath -Force
            }
        }
    }
    # Write-Host "Update DX version to $dxVersion"
    
    # $packageReferences | Where-Object { $_.Include -like "DevExpress*" } | ForEach-Object {
    #     Write-Host "Change $($_.Include) version to $dxVersion"
    #     $global:versionChanged=$true
    #     $_.Version = $dxVersion
        
    # }
    
    Get-Content $csprojPath -Raw

    $testAppDir = (Get-Item $testApplication).DirectoryName
    $shortDxVersion = Get-DevExpressVersion $dxVersion
    if (($dxVersion.ToCharArray() | Where-Object { $_ -eq "." }).Count -eq 2) {
        $dxVersion += ".0"
    }
    Get-ChildItem $testAppDir -Include "*.aspx", "*.config" -Recurse | ForEach-Object {
        $xml = Get-Content $_.FullName -Raw
        $regex = [regex] '(?<name>DevExpress.*)v\d{2}\.\d{1,2}(.*)Version=([.\d]*)'
        $result = $regex.Replace($xml, "`${name}v$shortDxVersion`$1Version=$dxversion")
        Set-Content $_.FullName $result.Trim()
    }
    
}
Get-ChildItem "$root\src\Tests\All\" *.csproj -Recurse | ForEach-Object {
    UpdateVersion $_.FullName $dxVersion $testApplication
}
Set-Location "$root\src\Tests\All"
Invoke-Script {
    if ($global:versionChanged){
        Write-Host "Paket Install" -f Green
        Invoke-PaketInstall
    }
    else{
        Write-Host "Paket Restore" -f Green
        if (Get-ChildItem ".\packages"){
            Invoke-PaketRestore
        }else{
            Invoke-PaketRestore -Force
        }
        
    }
}

Write-Host "Building TestApplication" -f Green

$localSource = "$root\bin\Nupkg"
$source = "$localSource;$(Get-PackageFeed -Nuget);$(Get-PackageFeed -Xpand);$source"
"Source=$source"
$testAppPAth = (Get-Item $testApplication).DirectoryName
Invoke-Script {
    & (Get-NugetPath) restore "$testAppPAth\TestApplication.Web\TestApplication.Web.csproj" -source $source
    & (Get-MsBuildPath) "$testAppPAth\TestApplication.Web\TestApplication.Web.csproj" /bl:$root\bin\TestWebApplication.binlog /WarnAsError /v:m -t:rebuild
}
Invoke-Script {
    & (Get-NugetPath) restore "$testAppPAth\TestApplication.Web\TestApplication.Web.csproj" -source $source
    & (Get-MsBuildPath) "$testAppPAth\TestApplication.Web\TestApplication.Web.csproj" /bl:$root\bin\TestWebApplication.binlog /WarnAsError /v:m -t:rebuild
}


