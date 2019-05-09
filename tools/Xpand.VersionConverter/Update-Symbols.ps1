function Update-Symbols {
    [CmdletBinding()]
    param (
        [parameter(Mandatory, ValueFromPipeline)]
        [System.IO.FileInfo]$pdb,
        [parameter(Mandatory)]
        [string]$TargetRoot,
        [parameter()]
        [string]$SourcesRoot,
        [parameter()]
        [string]$dbgToolsPath = "$PSScriptRoot\..\..\Private\srcsrv"
    )
    
    begin {
        if (!(test-path $dbgToolsPath)) {
            throw "srcsrv is invalid"
        }
        $remoteTarget = ($TargetRoot -like "http*")
        if (!$remoteTarget ){
            if (!$SourcesRoot.EndsWith("\")){
                $SourcesRoot+="\"
            }
            if (!$TargetRoot.EndsWith("\")){
                $TargetRoot+="\"
            }
        }
        $list = New-Object System.Collections.ArrayList
    }
    
    process {
        $list.Add($pdb)|Out-Null
    }
    
    end {
        Write-Verbose "Indexing $($list.count) pdb files"
        $list|foreach {
        # $list|foreach {
            Write-Verbose "Indexing $($_.FullName) ..."
        
            $streamPath = [System.IO.Path]::GetTempFileName()
            Write-Verbose "Preparing stream header section..."
            Add-Content -value "SRCSRV: ini ------------------------------------------------" -path $streamPath
            Add-Content -value "VERSION=1" -path $streamPath
            Add-Content -value "INDEXVERSION=2" -path $streamPath
            Add-Content -value "VERCTL=Archive" -path $streamPath
            Add-Content -value ("DATETIME=" + ([System.DateTime]::Now)) -path $streamPath

            Write-Verbose "Preparing stream variables section..."
            Add-Content -value "SRCSRV: variables ------------------------------------------" -path $streamPath
        
            if ($remoteTarget) {
                Add-Content -value "SRCSRVVERCTRL=http" -path $streamPath
            }
            Add-Content -value "SRCSRVTRG=%var2%" -path $streamPath
            Add-Content -value "SRCSRVCMD=" -path $streamPath

            Write-Verbose "Preparing stream source files section..."
                
            $sources = & "$dbgToolsPath\srctool.exe" -r $_.FullName |Select-Object -SkipLast 1
            if ($sources) {
                Add-Content -value "SRCSRV: source files ---------------------------------------" -path $streamPath
                foreach ($src in $sources) {
                    $target = "$src*$TargetRoot$src"
                    if ($remoteTarget) {
                        $file = "$src".replace($SourcesRoot, '').Trim("\").replace("\", "/")
                        $target = "$src*$TargetRoot/$file"
                    }
                    Add-Content -value $target -path $streamPath
                    Write-Verbose "Indexing $src to $target"
                }
                Add-Content -value "SRCSRV: end ------------------------------------------------" -path $streamPath
                
                $pdbstrPath = "$dbgToolsPath\pdbstr.exe"
                Write-Verbose "Saving the generated stream into the PDB file..."
                & $pdbstrPath -w -s:srcsrv "-p:$($_.Fullname)" "-i:$streamPath"
                Write-Verbose "Done."
                Remove-Item $streamPath
                Write-Verbose  $_ 
            }
            else {
                throw "No steppable code in pdb file $_, skipping"
            }

            
        }
    }
}
