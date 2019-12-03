param(
    $Branch = "lab",
    $sourcesRoot = "$PSScriptRoot\..\..",
    $apiKey,
    $localPackageSource = "$PSScriptRoot\..\..\bin\Nupkg",
    $PastBuild,
    $criteria = "Xpand.*"

)

"localPackageSource=$localPackageSource"
if (!(Get-Module XpandPwsh -ListAvailable)){
    Install-Module XpandPwsh -Force
}

New-Item $sourcesRoot\build\Nuget -ItemType Directory
$localPackages=Get-ChildItem "$localPackageSource" 
Write-HostFormatted "local-packages:" -Section
$localPackages

$pArgs = @{
    PackageSource = "Release"
    Filter=$criteria
}
$remotePackageSource=Get-PackageFeed -Nuget
if ($Branch -eq "lab") {
    $pArgs.PackageSource="Lab"
    $remotePackageSource=Get-PackageFeed -Xpand
}
$args
$packages =Find-XpandPackage  @pArgs

Write-HostFormatted "remote-packages:" -Section
$packages|Format-Table -AutoSize


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
        $clearCache=$true
    }
    else{
        Remove-Item $_ -Verbose
    }
}
if ($clearCache){
    Invoke-RestMethod "https://xpandnugetstats.azurewebsites.net/api/totals/clearcache"
}
