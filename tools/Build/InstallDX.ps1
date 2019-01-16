param($binPath = "$PSScriptRoot\..\..\bin", $nugetExe = "$PSScriptRoot\..\NuGet.exe", $dxSource = 'C:\Program Files (x86)\DevExpress 18.2\Components\System\Components\packages')
$ErrorActionPreference = "Stop"
$tempPath="$binPath\TempDXNupkg"
"Installing DX assemblies from $dxSource"
New-Item $tempPath -ItemType Directory -Force|Out-Null
$temp =(Get-Item $tempPath).FullName

$nugets =& "$PSScriptRoot\CollectDXNugets.ps1"
workflow Install-AllDXNugets{
    param($psObj)
    $complete=0
    Foreach -parallel ($nuget in $psObj.Nugets)    { 
        InlineScript {
            Import-Module "$($Using:psObj.ScriptRoot)\XpandPosh.psm1" -Force 
            Write-Output "Installing $($Using:nuget.Name) nuget"
            Invoke-Retry{
                & $Using:psObj.NugetExe Install $Using:nuget.Name -source "$($Using:psObj.Source);https://xpandnugetserver.azurewebsites.net/nuget" -OutputDirectory $Using:psObj.OutputDirectory
            }
            
        } 
        $Workflow:complete=$Workflow:complete+1 
        $percentComplete=($Workflow:complete*100)/$Workflow:psObj.Nugets.Count
        Write-Progress -Id 1 -Activity $nuget.Name -PercentComplete $percentComplete
    }
}
$psObj=[PSCustomObject]@{
    OutputDirectory = $(Get-Item $temp).FullName
    Source=$dxSource
    NugetExe=(Get-Item $nugetExe).FullName
    Nugets=$nugets
    ScriptRoot=$PSScriptRoot
}
Install-AllDXNugets -psObj $psObj
"Flattening nugets..."
Get-ChildItem -Path "$tempPath" -Include "*.dll" -Recurse  |Where-Object{
    $item=Get-Item $_
    $item.GetType().Name -eq "FileInfo" -and $item.DirectoryName -like "*net452"
}|Copy-Item -Destination $binPath -Force 
Remove-Item $temp -Recurse -force
