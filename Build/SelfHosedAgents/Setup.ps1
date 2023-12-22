param(
    $templateVHDPath = "D:\Hyper-V\Win-Test\Win-Test\Virtual Hard Disks\Win-Test.vhdx", 
    $newVMName = "Agent",                                   
    $newVHDPath = "D:\Hyper-V\AzureDevOpsAgents\$newVMName\$newVMName.vhdx",
    $vmMemory = 4096,
    $proccessor = 2,
    $vmSwitch = "Default Switch",                            
    $user = "Admin", 
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
}

Start-VMJobs -numberOfVMs $numberOfVMs -scriptBlock $setupBlock -vmName $newVMName 


