param(
    $Branch = "master",
    $sourcesRoot = "$PSScriptRoot\..\..",
    $apiKey,
    $localPackageSource = "$PSScriptRoot\..\..\bin\Nupkg",
    $criteria = "Xpand.*"
)
if (!(Get-Module XpandPwsh -ListAvailable)){
    Install-Module XpandPwsh -Force
}

$remotePackageSource=Get-PackageFeed -Nuget

if ($Branch -eq "lab"){
    $remotePackageSource=Get-PackageFeed -Xpand
}
set-location $sourcesRoot
$pArgs = @{
    PackageSource = "Release"
    Filter=$criteria
}
if ($Branch -eq "lab") {
    $pArgs.PackageSource="Lab"
}
$packages =Find-XpandPackage  @pArgs

Write-Host "remote-packages:" -f blue
$packages
$localPackages=Get-ChildItem $localPackageSource *.nupkg -Recurse | Sort-Object -Unique
Write-Host "local-packages:" -f blue
$localPackages
$localPackages| ForEach-Object {
    $localPackageName = [System.IO.Path]::GetFileNameWithoutExtension($_)
    $r = New-Object System.Text.RegularExpressions.Regex("[\d]{1,2}\.[\d]{1}\.[\d]*(\.[\d]*)?")
    $localPackageVersion = $r.Match($localPackageName).Value
    $localPackageName = $localPackageName.Replace($localPackageVersion, "").Trim(".")
    "localPackage=$localPackageName, $localPackageVersion"
    
    $package = $packages | Where-Object { $_.Id -eq $localPackageName }
    "publishedPackage=$package"
    if (!$package -or (([version]$package.Version) -lt ([version]$localPackageVersion))) {
        "Pushing $($_.FullName)"
        & (Get-Nugetpath) push $_.FullName -source $remotePackageSource -ApiKey $apikey
    }
}
