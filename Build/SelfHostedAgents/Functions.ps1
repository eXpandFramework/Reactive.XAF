function Approve-Parameters{
    param(
        [parameter(Mandatory)]
        $invocation
    )
    foreach ($param in $invocation.MyCommand.Parameters.GetEnumerator()) {
        if ($param.Value.ParameterType -ne [System.Management.Automation.SwitchParameter]){
            if (!(Get-Variable -Name $param.Key -ValueOnly)) {
                throw "Parameter $($param.Key) is required and cannot be null or empty."
            }
        }
    }
}
function Wait-VMReadiness {
    param (
        [parameter(Mandatory)]
        [string]$vmName,
        [parameter(Mandatory)]
        [PSCredential]$cred,
        [int]$timeoutSeconds = 600 
    )

    $startTime = Get-Date

    do {
        $currentTime = Get-Date
        $elapsedTime = ($currentTime - $startTime).TotalSeconds
        if ($elapsedTime -gt $timeoutSeconds) {
            Write-Host "Timeout reached. Exiting wait loop."
            return $false
        }

        Start-Sleep -Seconds 5
        try {
            $session = New-PSSession -VMName $vmName -Credential $cred -ErrorAction Stop
            if ($session) {
                $isReady = Invoke-Command -Session $session -ScriptBlock {
                    (Get-Service -Name 'wuauserv').Status -eq 'Running' -and 
                    (Get-Service -Name "LanmanServer").Status -eq "Running"
                }
                Remove-PSSession $session
                if ($isReady) {
                    Start-Sleep -Seconds 10
                    $session = New-PSSession -VMName $vmName -Credential $cred -ErrorAction Stop
                    $isReady = Invoke-Command -Session $session -ScriptBlock {
                        (Get-Service -Name 'wuauserv').Status -eq 'Running' -and 
                        (Get-Service -Name "LanmanServer").Status -eq "Running"
                    }
                    Remove-PSSession $session
                    if ($IsReady){
                        Write-Host "VM is ready."
                        return $true
                    }
                }
            }
        }
        catch {
            Write-Host "Waiting for VM WinRM service and system readiness..."
        }
    } while ($true)
}
function Remove-AgentVM{
    param(
        $newVMName
    )
    $newVMName|Get-VM -ErrorAction SilentlyContinue|ForEach-Object{
        Stop-VM -Name $_.Name -Force -TurnOff
        Remove-VM -Name $_.name -Force
    }
}
function Initialize-AgentVM{
param(
    [parameter(Mandatory)]
    $newVMName,
    [parameter(Mandatory)]
    $newVHDPath,
    [parameter(Mandatory)]
    $vmMemory,
    [parameter(Mandatory)]
    $templateVHDPath,
    [parameter(Mandatory)]
    [pscredential]$cred,
    [parameter(Mandatory)]
    $vmSwitch,
    [parameter(Mandatory)]
    $proccessor
)
    
    
    New-VM -Name $newVMName -MemoryStartupBytes "$($vmMemory)MB" -Generation 2 -NoVHD -SwitchName $vmSwitch
    
    # Set-VMMemory -VMName $newVMName -DynamicMemoryEnabled $true -MinimumBytes "$($vmMemory/2)MB" -StartupBytes "$($vmMemory/2)MB" -MaximumBytes "$($vmMemory)MB"
    Set-VMMemory -VMName $newVMName -StartupBytes "$($vmMemory)MB" -DynamicMemoryEnabled $false 
    
    Set-VMProcessor -VMName $newVmName -Count $proccessor
    $parentVHDPath = $templateVHDPath
    $differencingDiskPath = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($newVHDPath), $newVMName + "_Diff" + [System.IO.Path]::GetExtension($newVHDPath))
    if (Test-path $differencingDiskPath){
        Remove-Item $differencingDiskPath
    }
    New-VHD -Path $differencingDiskPath -ParentPath $parentVHDPath -Differencing
    Add-VMHardDiskDrive -VMName $newVMName -Path $differencingDiskPath
    
    Start-VM -Name $newVMName
    Wait-VMReadiness -vmName $newVMName -cred $cred 
    
    Rename-VMComputer $newVMName $cred
    Wait-VMReadiness -vmName $newVMName -cred $cred 
}


function Remove-AzureAgent{
    param(
        [parameter(Mandatory)]
        $token,
        [parameter(Mandatory)]
        $organization,
        [parameter(Mandatory)]
        $newVMName,
        [parameter(Mandatory)]
        $agentPool
    )
    $headers = @{
        Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$token"))
    }
    $apiUrl = "https://dev.azure.com/$organization/_apis/distributedtask/pools?api-version=6.0"
    $response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers
    $pools = $response.value
    $poolId = $pools | Where-Object { $_.name -eq $agentPool } | Select-Object -ExpandProperty id
    
    $apiUrl = "https://dev.azure.com/$organization/_apis/distributedtask/pools/$poolId/agents?api-version=6.0"
    $response = Invoke-RestMethod -Uri $apiUrl -Method Get -Headers $headers
    $agents = $response.value
    $agentId = $agents | Where-Object { $_.name -eq $newVMName } | Select-Object -ExpandProperty id
    
    if ($agentId){
        $apiUrl = "https://dev.azure.com/{0}/_apis/distributedtask/pools/{1}/agents/{2}?api-version=6.0" -f $organization, $poolId, $agentId
        Invoke-RestMethod -Uri $apiUrl -Method Delete -Headers $headers
    }
}

function Install-AzureAgent{
    param(
        [parameter(Mandatory)]
        $downloadUrl,
        [parameter(Mandatory)]
        $newVMName,
        [parameter(Mandatory)]
        [pscredential]$cred,
        [parameter(Mandatory)]
        $token,
        [parameter(Mandatory)]
        $organization,
        [parameter(Mandatory)]
        $agentPool
    )
    
    Remove-AzureAgent $token $organization $newVMName $agentPool

    $scriptBlock = {
        param($downloadUrl)
        $destinationPath = Join-Path $env:TEMP $downloadUrl.Split("/")[-1]
        Invoke-WebRequest -Uri $downloadUrl -OutFile $destinationPath
        if (!(Test-Path c:\agent)){
            New-Item -ItemType Directory -Name agent -Path c:\ 
        }
        Set-Location c:\agent
        Add-Type -AssemblyName System.IO.Compression.FileSystem ; [System.IO.Compression.ZipFile]::ExtractToDirectory($destinationPath, "$PWD")
    }
    
    Invoke-Command -VMName $newVMName -ScriptBlock $scriptBlock -ArgumentList $downloadUrl -Credential $cred
}

function Register-Agent{
    param(
        [parameter(Mandatory)]
        $organization,
        [parameter(Mandatory)]
        $token,
        [parameter(Mandatory)]
        $newVMName,
        [parameter(Mandatory)]
        $agentPool,
        [parameter(Mandatory)]
        [PsCredential]$cred,
        [parameter(Mandatory)]
        $user,
        [parameter(Mandatory)]
        $pass
    )
    $agentConfigParams = @{
        AzureDevOpsUrl = "https://dev.azure.com/$organization" 
        PersonalAccessToken = $token
        AgentName = $newVMName 
        AgentPool = $agentPool
        WorkFolder = "_work" 
        User=$user
        Pass=$pass
    }
    
    $scriptBlock = {
        param($configParams)
        Set-Location c:\agent
    
        $arguments = @(
            "--unattended",
            "--url", $configParams.AzureDevOpsUrl,
            "--auth", "pat",
            "--token", $configParams.PersonalAccessToken,
            "--pool", $configParams.AgentPool,
            "--agent", $configParams.AgentName,
            "--acceptTeeEula",
            "--runAsAutoLogon", 
            "--windowsLogonAccount", $configParams.user,
            "--windowsLogonPassword", $configParams.pass
        )
        & .\config.cmd @arguments
    }
    
    $result=Invoke-Command -VMName $newVmName -ScriptBlock $scriptBlock -ArgumentList $agentConfigParams -Credential $cred
    Write-Host $result
}

function Rename-VMComputer{
    param(
        [parameter(Mandatory)]
        $newVMName,
        [parameter(Mandatory)]
        [pscredential]$cred
    )
    Invoke-Command -VMName $newVMName -Credential $cred -ScriptBlock {
        Rename-Computer -NewName $using:newVMName -Force -Restart
    } 
}

function Start-VMJobs {
    param(
        [parameter(Mandatory)]
        $vmName,
        [parameter(Mandatory)]
        [int]$numberOfVMs ,
        [parameter(Mandatory)]
        [scriptblock]$scriptBlock
    )

    $paramSets = 1..$numberOfVMs | ForEach-Object {
        @{ newVMName = "$vmName$_"; verbose = $VerbosePreference }
    }
    
    $jobs = @()
    foreach ($params in $paramSets) {
        $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $params.newVMName
        $jobs += $job
    }

    $totalJobs = $jobs.Count
    $completedJobs = 0

    while ($completedJobs -lt $totalJobs) {
        $completedJobs = ($jobs | Where-Object { $_.State -eq 'Completed' }).Count
        $progress = ($completedJobs / $totalJobs) * 100
        Write-Progress -Activity "Running Jobs" -Status "$completedJobs out of $totalJobs completed" -PercentComplete $progress
        Start-Sleep -Seconds 5
    }

    foreach ($job in $jobs) {
        $result = Receive-Job -Job $job 
        Write-Host "Results for job $($job.Id):"
        Write-Host $result
    }
    $jobs | Remove-Job

    Write-Progress -Id 0 -Activity "Running Jobs" -Status "All jobs completed" -Completed

}

function Start-SysPrep{
    param(
        [parameter(Mandatory)]
        $newVMName,
        [parameter(Mandatory)]
        $user,
        [parameter(Mandatory)]
        $pass,
        [parameter(Mandatory)]
        $masterVMName,
        [parameter(Mandatory)]
        $sysprepAnswersFile
    )
    Get-VM | Where-Object { $_.Name -like "$newVMName*" -and $_.State -eq "Running"}|ForEach-Object{
        Stop-VM -Name $_.Name -TurnOff
    }
    $cred = New-Object System.Management.Automation.PSCredential($user, $(ConvertTo-SecureString $pass -AsPlainText -Force))
    if ((Get-VM -Name $masterVMName).state -ne "Running"){
        Start-VM -Name $masterVMName
        Wait-VMReadiness -vmName $masterVMName -cred $cred
    }

    $unattendPath="C:\Windows\System32\Sysprep\Unattend.xml"
    Copy-Item -Path $sysprepAnswersFile -Destination $unattendPath -ToSession (New-PSSession -VMName $masterVMName -Credential $cred) -Force
    
    Invoke-Command -VMName $masterVMName -ScriptBlock {
        $content = Get-Content -Path $using:unattendPath -Raw

        $content = $content -replace '(<AutoLogon>.*?<Username>).*?(</Username>)', "`$1$using:user`$2"
        $content = $content -replace '(<AutoLogon>.*?<Password>.*?<Value>).*?(</Value>)', "`$1$using:pass`$2"
        $content = $content -replace '(<AutoLogon>.*?<Password>.*?<PlainText>).*?(</PlainText>)', "`$1true`$2"

        $content = $content -replace '(<UserAccounts>.*?<AdministratorPassword>.*?<Value>).*?(</Value>)', "`$1$using:pass`$2"
        $content = $content -replace '(<UserAccounts>.*?<AdministratorPassword>.*?<PlainText>).*?(</PlainText>)', "`$1true`$2"

        C:\Windows\System32\Sysprep\sysprep.exe /generalize /oobe /shutdown /unattend:C:\Windows\System32\Sysprep\unattend.xml
    } -Credential $cred
    
    do {
        $vm = Get-VM -Name $masterVMName
        Start-Sleep -Seconds 2 
    } while ($vm.State -ne 'Off')
}