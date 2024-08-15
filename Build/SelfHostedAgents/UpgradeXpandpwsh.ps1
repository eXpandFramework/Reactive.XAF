param(
    $VMName = $env:AgentVmName,                                   
    $numberOfVMs = $env:AzAgentCount,
    $pass = $env:agentPass,
    $user = $env:AgentUser,
    $packageSource="..\..\bin\nupkg\",
    $VMNamePackagesPath=$env:AgentPackagesPath,
    [switch]$KeepCache
)
$functionsScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "functions.ps1" 
. $functionsScriptPath
# $scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
# $packageSource = Join-Path -Path $scriptDirectory -ChildPath "..\..\bin\nupkg\"
$packageSource = Resolve-Path -Path $packageSource

$setupBlock = {
    param($newVMName)
    . $using:functionsScriptPath
    $vm = Get-VM -Name $newVMName -ErrorAction SilentlyContinue
    if ($vm -ne $null) {
        
        $cred = New-Object System.Management.Automation.PSCredential($using:user, $(ConvertTo-SecureString $using:pass -AsPlainText -Force))

        Invoke-Command -VMName $newVMName -Credential $cred -ScriptBlock {
            param($innerClearCache)
            get-module xpandpwsh -ListAvailable|Uninstall-Module -Force    
        } -ArgumentList $clearCache 
        
    }
}



Start-VMJobs -numberOfVMs $numberOfVMs -scriptBlock $setupBlock -vmName $VMName
