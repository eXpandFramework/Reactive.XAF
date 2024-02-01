param(
    $VMName = "Agent",                                   
    $numberOfVMs = $env:AzAgentCount,
    $pass = $env:agentPass,
    $user = $env:AgentUser,
    $packageSource="..\..\bin\nupkg\",
    [switch]$KeepCache
)
$functionsScriptPath = Join-Path -Path $PSScriptRoot -ChildPath "functions.ps1" 
. $functionsScriptPath
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$packageSource = Join-Path -Path $scriptDirectory -ChildPath "..\..\bin\nupkg\"
$packageSource = Resolve-Path -Path $packageSource

$setupBlock = {
    param($newVMName)
    . $using:functionsScriptPath
    $vm = Get-VM -Name $newVMName -ErrorAction SilentlyContinue
    if ($vm -ne $null) {
        $clearCache = !$using:KeepCache.IsPresent
        $cred = New-Object System.Management.Automation.PSCredential($using:user, $(ConvertTo-SecureString $using:pass -AsPlainText -Force))
        $packages = Get-ChildItem $using:packageSource
        Invoke-Command -VMName $newVMName -Credential $cred -ScriptBlock {
            param($innerClearCache)
            New-Item -Path 'C:\AgentPackages' -ItemType Directory -ErrorAction SilentlyContinue
            if (!(dotnet nuget list source | Select-String AgentPackages)){
                dotnet nuget add source c:\AgentPackages -n AgentPackages
            }
            if ($innerClearCache){
                dotnet nuget locals all --clear
                Get-ChildItem c:\agent -Filter ".nuget" -Recurse | Remove-Item -Recurse -Force
            }
        } -ArgumentList $clearCache 
        $packages | Copy-Item -Destination c:\AgentPackages -ToSession (New-PSSession -VMName $newVmname -Credential $cred) -Force    
    }
}



Start-VMJobs -numberOfVMs $numberOfVMs -scriptBlock $setupBlock -vmName $VMName 


