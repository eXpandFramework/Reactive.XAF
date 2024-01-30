<#
.SYNOPSIS
This script automates the setup of Hyper-V virtual machines, installs and configures Azure DevOps agents, and applies specific system settings.

.DESCRIPTION
The script performs several tasks related to configuring and managing Hyper-V virtual machines and Azure DevOps agents. It includes parameters for specifying VM details, paths, credentials, and Azure DevOps agent configurations. The script ensures all parameters are provided and valid. If the Sysprep switch is used, it prepares the VM using Sysprep. It also includes a script block for initializing VMs, installing and configuring Azure DevOps agents, and applying system settings. The script assumes the existence of a separate script ('functions.ps1') that contains necessary functions.

.PARAMETER templateVHDPath
Path to the template virtual hard disk (VHD).

.PARAMETER newVMName
Name of the new virtual machine.

.PARAMETER newVHDPath
Path for the new virtual machine's VHD.

.PARAMETER vmMemory
Amount of memory (in MB) allocated to the virtual machine.

.PARAMETER proccessor
Number of processors allocated to the virtual machine.

.PARAMETER vmSwitch
Name of the virtual switch to be used by the virtual machine.

.PARAMETER user
Username for the virtual machine.

.PARAMETER downloadUrl
URL for downloading the Azure DevOps agent package.

.PARAMETER organization
Azure DevOps organization name.

.PARAMETER token
Personal Access Token for Azure DevOps.

.PARAMETER agentPool
Azure DevOps agent pool name.

.PARAMETER pass
Password for the virtual machine user.

.PARAMETER numberOfVMs
Number of virtual machines to create.

.PARAMETER masterVMName
Name of the master virtual machine (used for Sysprep).

.PARAMETER Sysprep
Switch to indicate whether Sysprep should be used.

.PARAMETER sysprepAnswersFile
Path to the Sysprep answers file.

.EXAMPLE
.\Setup.ps1 -templateVHDPath "path" -newVMName "VMName" -vmMemory 4096

This example runs the script with specified template VHD path, new VM name, and VM memory size.

.NOTES
- Requires 'functions.ps1' script containing definitions of Start-SysPrep, Remove-AgentVM, Initialize-AgentVM, Install-AzureAgent, Register-Agent, and Start-VMJobs.
- Ensure all parameters are correctly set before running the script.
- The script is designed for use in environments where Hyper-V and Azure DevOps are used.
#>

param(
    $templateVHDPath = "D:\Hyper-V\Win-Test\Win-Test\Virtual Hard Disks\Win-Test.vhdx", 
    # $templateVHDPath , 
    $newVMName = "Agent",                                   
    $newVHDPath = "D:\Hyper-V\AzureDevOpsAgents\$newVMName\$newVMName.vhdx",
    $vmMemory = 4096,
    $proccessor = 2,
    $vmSwitch = "Default Switch",                            
    $user = $env:AgentUser, 
    $downloadUrl = "https://vstsagentpackage.azureedge.net/agent/3.230.0/vsts-agent-win-x64-3.230.0.zip",
    $organization = "eXpandDevOps",
    $token = $env:AzureToken,
    $agentPool = "Self",
    $pass = $env:agentPass,
    $numberOfVMs = $env:AzAgentCount,
    $masterVMName="Win-Test",
    [Switch]$Sysprep,
    $sysprepAnswersFile=".\unattend.xml"
)


$functionsScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "functions.ps1" 
. $functionsScriptPath
Approve-Parameters $MyInvocation

if ($Sysprep ){
    Start-SysPrep -newVMName $newVMName -user $user -pass $pass -masterVMName $masterVMName -sysprepAnswersFile $sysprepAnswersFile
}

$setupBlock = {
    param($newVMName)
    . $using:functionsScriptPath
    Remove-AgentVM -newVMName $newVMName 
    $cred = New-Object System.Management.Automation.PSCredential($using:user, $(ConvertTo-SecureString $using:pass -AsPlainText -Force))
    Initialize-AgentVM $newVMName $using:newVHDPath $using:vmMemory $using:templateVHDPath $cred $using:vmSwitch $using:proccessor
    Install-AzureAgent $using:downloadUrl $newVMName $cred $using:token $using:organization $using:agentPool 
    Register-Agent $using:organization $using:token $newVMName $using:agentPool $cred $using:user $using:pass
    Invoke-Command -VMName $newVMName -Credential $cred -ScriptBlock {
        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'Hidden' -Value 1
        Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced' -Name 'HideFileExt' -Value 0
        Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private
    }
}

Start-VMJobs -numberOfVMs $numberOfVMs -scriptBlock $setupBlock -vmName $newVMName 


