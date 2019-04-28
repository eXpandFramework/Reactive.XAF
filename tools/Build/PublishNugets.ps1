param(
    $Branch = "lab",
    $sourcesRoot = "$PSScriptRoot\..\..",
    $apiKey,
    $criteria = "Xpand.*",
    $localPackageSource = "$PSScriptRoot\..\..\bin\Nupkg"
)
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

Write-Host "packages:" -f blue
$packages
Get-ChildItem $localPackageSource *.nupkg -Recurse | ForEach-Object {
    $localPackageName = [System.IO.Path]::GetFileNameWithoutExtension($_)
    $r = New-Object System.Text.RegularExpressions.Regex("[\d]{1,2}\.[\d]{1}\.[\d]*(\.[\d]*)?")
    $localPackageVersion = $r.Match($localPackageName).Value
    "localPackageVersion=$localPackageVersion"
    $localPackageName = $localPackageName.Replace($localPackageVersion, "").Trim(".")
    "localPackageName=$localPackageName"
    $package = $packages | Where-Object { $_.Id -eq $localPackageName }
    "publishedPackage=$package"
    if (!$package -or (([version]$package.Version) -lt ([version]$localPackageVersion))) {
        "Pushing $($_.FullName)"
        & (Get-Nugetpath) push $_.FullName -source $remotePackageSource -ApiKey $apikey
    }
}
