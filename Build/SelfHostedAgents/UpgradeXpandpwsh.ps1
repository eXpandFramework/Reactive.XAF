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
            param($innerClearCache,$name)
            Write-Host "[$Name] Running as user: $(whoami)"
            
            # MODIFICATION: START
            # 1. Target PowerShell Core path explicitly since we are inside a 5.1 session
            $coreModulePath = Join-Path $home "Documents\PowerShell\Modules\XpandPwsh"
            if (Test-Path $coreModulePath) {
                Write-Host "[$Name] Detected PowerShell Core module at: $coreModulePath"
                Write-Host "[$Name] Removing directory directly..."
                Remove-Item -Path $coreModulePath -Recurse -Force -ErrorAction Stop
            }

            # 2. Check Windows PowerShell 5.1 path (Standard Uninstall)
            $winModule = Get-Module xpandpwsh -ListAvailable
            if ($winModule) {
                Write-Host "[$Name] Detected Windows PowerShell module at: $($winModule.Path)"
                $winModule | Uninstall-Module -Force -ErrorAction Stop
            }
            # MODIFICATION: END

            # Verification
            if (Test-Path $coreModulePath) {
                 Write-Output "Failed to remove Core module from $Name"
            }
            elseif (Get-Module xpandpwsh -ListAvailable) {
                 Write-Output "Failed to uninstall Windows module from $Name"
            }
            else {
                 Write-Output "Uninstalled from $Name"
                 
                 # MODIFICATION: START
                 # 1. Use pwsh to install to the Core path (Agent uses Core, this session is WinPS)
                 # 2. Use -NonInteractive to prevent the job from hanging on prompts
                 if (Get-Command pwsh -ErrorAction SilentlyContinue) {
                     $installCmd = "
                        Write-Host 'Bootstrapping NuGet...'
                        Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Scope CurrentUser -ErrorAction SilentlyContinue
                        
                        Write-Host 'Trusting PSGallery...'
                        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
                        
                        Write-Host 'Installing XpandPwsh...'
                        Install-Module XpandPwsh -Scope CurrentUser -Force -AllowClobber
                     "
                     
                     # Execute in Core, fail if prompts appear
                     pwsh -NonInteractive -Command $installCmd
                     
                     if ($LASTEXITCODE -eq 0) { 
                        Write-Output "Installed in $Name (via pwsh)" 
                     } else { 
                        Write-Error "Installation failed in $Name. Check if pwsh needs configuration." 
                     }
                 } 
                 else {
                     # Fallback for systems without Core (likely not your case, but safe to have)
                     Write-Warning "pwsh not found on $Name. Attempting WinPS install (may hang if prompts occur)."
                     Install-PackageProvider -Name NuGet -Force -Scope CurrentUser -ErrorAction SilentlyContinue
                     Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
                     Install-Module XpandPwsh -Scope CurrentUser -Force -AllowClobber
                     Write-Output "Installed in $Name (via WinPS)"
                 }
            }
        } -ArgumentList $clearCache, $newVMName
        
    }
}



Start-VMJobs -numberOfVMs $numberOfVMs -scriptBlock $setupBlock -vmName $VMName
