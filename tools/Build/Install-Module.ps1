param(
    [pscustomObject]$psObj
)
$module=Get-module $psObj.Name -ListAvailable|Where-Object{$_.Version -eq $psObj.Version}
if (!$module){
    $name=$psObj.Name
    $version=$psObj.Version
    Write-Warning "-> Installing $name $version"
    Install-Module $name -RequiredVersion $version -Scope CurrentUser -AllowClobber -Force
}
Import-Module $psObj.Name -Global -Prefix X -RequiredVersion $psObj.Version -Force 

