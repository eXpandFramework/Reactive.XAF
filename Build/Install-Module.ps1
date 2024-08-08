param([pscustomObject]$psObj)

$allVersions = Get-Module $psObj.Name -ListAvailable
$allVersions | Where-Object {$_.Version -ne $psObj.Version} | ForEach-Object {
    Write-Host "Uninstalling $($_.Name) $($_.Version)"
    Uninstall-Module $_.Name -RequiredVersion $_.Version -Force
}

$module = Get-Module $psObj.Name -ListAvailable | Where-Object {$_.Version -eq $psObj.Version}

if (!$module) {
    $name = $psObj.Name
    $version = $psObj.Version
    Write-Host "Installing $name $version"
    Install-Module $name -RequiredVersion $version -Scope CurrentUser -AllowClobber -Force
}

Import-Module $psObj.Name -Global -Prefix X -RequiredVersion $psObj.Version -Force