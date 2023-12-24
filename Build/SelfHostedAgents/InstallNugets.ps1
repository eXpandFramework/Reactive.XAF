param(
    $VMName = "Agent",                                   
    $numberOfVMs = $env:AzAgentCount,
    $pass = $env:agentPass,
    $user = "Admin",
    $packageSource="..\..\bin\nupkg\",
    [switch]$KeepCache
)
$functionsScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "functions.ps1" 
. $functionsScriptPath


$setupBlock = {
    param($newVMName)
    . $using:functionsScriptPath
    $clearCache=!$using:KeepCache.IsPresent
    $cred = New-Object System.Management.Automation.PSCredential($using:user, $(ConvertTo-SecureString $using:pass -AsPlainText -Force))
    $packages=Get-ChildItem $using:packageSource
    Invoke-Command -VMName $newVMName -Credential $cred -ScriptBlock {
        New-Item -Path 'C:\AgentPackages' -ItemType Directory -ErrorAction SilentlyContinue
        if (!(dotnet nuget list source|Select-String AgentPackages)){
            dotnet nuget add source c:\AgentPackages -n AgentPackages
        }
        if ($using:clearCache){
            dotnet nuget locals all --clear
            Get-ChildItem c:\agent -Filter ".nuget" -Recurse|Remove-Item -Recurse -Force
        }
    }
    $packages|Copy-Item -Destination c:\AgentPackages -ToSession (New-PSSession -VMName $newVmname -Credential $cred) -Force
    
}


Start-VMJobs -numberOfVMs $numberOfVMs -scriptBlock $setupBlock -vmName $VMName 


