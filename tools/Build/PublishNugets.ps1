param(
    $Branch = "lab",
    $sourcesRoot = "$PSScriptRoot\..\..",
    $apiKey,
    $localPackageSource = "$PSScriptRoot\..\..\bin\Nupkg",
    $PastBuild,
    $criteria = "Xpand.*"

)
$VerbosePreference="continue"
"PastBuild=$pastbuild"
"localPackageSource=$localPackageSource"
if ($PastBuild -and $PastBuild -ne "false"){
    return
}
$localPackages=Get-ChildItem "$localPackageSource" 
Write-Host "local-packages:`r`n$localPackages"

if (!(Get-Module XpandPwsh -ListAvailable)){
    Install-Module XpandPwsh -Force
}



$pArgs = @{
    PackageSource = "Release"
    Filter=$criteria
}
$remotePackageSource=Get-PackageFeed -Nuget
if ($Branch -eq "lab") {
    $pArgs.PackageSource="Lab"
    $remotePackageSource=Get-PackageFeed -Xpand
}
$packages =Find-XpandPackage  @pArgs

Write-Host "remote-packages:`r`n$packages"


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
