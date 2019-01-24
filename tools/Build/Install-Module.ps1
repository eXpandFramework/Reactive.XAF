param(
    [pscustomObject]$psObj
)
$module=Get-module $psObj.Name -ListAvailable|Where-Object{$_.Version -eq $psObj.Version}
if (!$module){
    Install-Module $psObj.Name -RequiredVersion $psObj.Version -Scope CurrentUser -AllowClobber -Force
}
Import-Module $psObj.Name -Global -Prefix X -RequiredVersion $psObj.Version -Force 

