function Install-MonoCecil($resolvePath) {
    Write-Verbose "Loading Mono.Cecil"
    $monoPath = "$PSScriptRoot\mono.cecil.0.10.3\lib\net40"
    if (!(Test-Path "$monoPath\Mono.Cecil.dll")) {
        $client = New-Object System.Net.WebClient
        $client.DownloadFile("https://www.nuget.org/api/v2/package/Mono.Cecil/0.10.3", "$PSScriptRoot\mono.cecil.0.10.3.zip")
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [ZipFile]::ExtractToDirectory("$PSScriptRoot\mono.cecil.0.10.3.zip", "$PSScriptRoot\mono.cecil.0.10.3")
    }

    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.dll")) | Out-Null
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.pdb.dll")) | Out-Null
    Add-Type @"
using Mono.Cecil;
public class MyDefaultAssemblyResolver : DefaultAssemblyResolver{
    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters){
        try{
            return base.Resolve(name, parameters);
        }
        catch (AssemblyResolutionException){
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(string.Format(@"$resolvePath\{0}.dll", name.Name));
            return assemblyDefinition;
        }
    }
}
"@ -ReferencedAssemblies @("$monoPath\Mono.Cecil.dll")
}
function Remove-OtherVersionFlags {
    param(
        $PackageDir,
        $DXVersion
    )
    Get-ChildItem $packageDir *VersionConverter.v.* -Exclude $dxVersion | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
}
function Use-Object {
    [CmdletBinding()]
    param (
        [Object]$InputObject,
        [Parameter(Mandatory = $true)]
        [scriptblock]$ScriptBlock
    )
    $killDomain
    try {
        . $ScriptBlock
    }
    catch {
        $killDomain = $true
        throw 
    }
    finally {
        if ($null -ne $InputObject -and $InputObject -is [System.IDisposable]) {
            $InputObject.Dispose()
            if ($killDomain) {
                # Stop-Process -id $pid
            }
        }
    }
}
function Get-MonoAssembly($path, [switch]$Write) {
    $readerParams = New-Object ReaderParameters
    if ($Write) {
        $readerParams.ReadWrite = $true
        $readerParams.ReadSymbols = $true
    }
    $readerParams.AssemblyResolver = New-Object MyDefaultAssemblyResolver
    [ModuleDefinition]::ReadModule($path, $readerParams).Assembly
}
function Get-DevExpressVersion($targetPath, $referenceFilter, $projectFile) {
    try {
        Write-Verbose "Locating DevExpress version..."
        $projectFileInfo = Get-Item $projectFile
        [xml]$csproj = Get-Content $projectFileInfo.FullName
        $packageReference = $csproj.Project.ItemGroup.PackageReference.Include | Where-Object { $_ -like "$referenceFilter" }
        if ($packageReference) {
            $v = ($packageReference ).Version | Select-Object -First 1
            if ($packageReference) {
                $version = [version]$v
            }
        }
        else {
            $references = $csproj.Project.ItemGroup.Reference
            $dxReferences = $references.Include | Where-Object { $_ -like "$referenceFilter" }    
            $hintPath = $dxReferences.HintPath | foreach-Object { 
                if ($_) {
                    $path = $_
                    if (![path]::IsPathRooted($path)) {
                        $path = "$((Get-Item $projectFile).DirectoryName)\$_"
                    }
                    if (Test-Path $path) {
                        [path]::GetFullPath($path)
                    }
                }
            } | Where-Object { $_ } | Select-Object -First 1
            if ($hintPath ) {
                Write-Verbose "$($dxAssembly.Name.Name) found from $hintpath"
                $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($hintPath).FileVersion
            }
            else {
                $dxAssemblyPath = Get-ChildItem $targetPath "$referenceFilter*.dll" | Select-Object -First 1
                if ($dxAssemblyPath) {
                    Write-Verbose "$($dxAssembly.Name.Name) found from $($dxAssemblyPath.FullName)"
                    $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssemblyPath.FullName).FileVersion
                }
                else {
                    $include = ($dxReferences | Select-Object -First 1)
                    Write-Verbose "Include=$Include"
                    $dxReference = [Regex]::Match($include, "DevExpress[^,]*", [RegexOptions]::IgnoreCase).Value
                    Write-Verbose "DxReference=$dxReference"
                    $dxAssembly = Get-ChildItem "$env:windir\Microsoft.NET\assembly\GAC_MSIL"  *.dll -Recurse | Where-Object { $_ -like "*$dxReference.dll" } | Select-Object -First 1
                    if ($dxAssembly) {
                        $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly.FullName).FileVersion
                    }
                }
            }
        }
        $version
    }
    catch {
        Write-Warning "$_`r`n$howToVerbose`r`n"
        throw "Check output warning message"
    }
}

function Get-DevExpressVersionFromReference {
    param(
        $csproj,
        $targetPath,
        $referenceFilter,
        $projectFile
    )
    $references = $csproj.Project.ItemGroup.Reference
    $dxReferences = $references | Where-Object { $_.Include -like "$referenceFilter" }    
    $hintPath = $dxReferences.HintPath | foreach-Object { 
        if ($_) {
            $path = $_
            if (![path]::IsPathRooted($path)) {
                $path = "$((Get-Item $projectFile).DirectoryName)\$_"
            }
            if (Test-Path $path) {
                [path]::GetFullPath($path)
            }
        }
    } | Where-Object { $_ } | Select-Object -First 1
    if ($hintPath ) {
        Write-Verbose "$($dxAssembly.Name.Name) found from $hintpath"
        [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($hintPath).FileVersion
    }
    else {
        $dxAssemblyPath = Get-ChildItem $targetPath "$referenceFilter*.dll" | Select-Object -First 1
        if ($dxAssemblyPath) {
            Write-Verbose "$($dxAssembly.Name.Name) found from $($dxAssemblyPath.FullName)"
            [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssemblyPath.FullName).FileVersion
        }
        else {
            $include = ($dxReferences | Select-Object -First 1).Include
            $dxReference = [Regex]::Match($include, "DevExpress[^,]*", [RegexOptions]::IgnoreCase).Value
            Write-Verbose "Include=$Include"
            Write-Verbose "DxReference=$dxReference"
            $dxAssembly = Get-ChildItem "$env:windir\Microsoft.NET\assembly\GAC_MSIL"  *.dll -Recurse | Where-Object { $_ -like "*$dxReference.dll" } | Select-Object -First 1
            if ($dxAssembly) {
                [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly.FullName).FileVersion
            }
            else {
                throw "Cannot find DevExpress Version"
            }
        }
    }
}
function Update-Version($modulePath, $dxVersion) {
    Use-Object($moduleAssembly = Get-MonoAssembly $modulePath -Write) {
        $moduleReferences = $moduleAssembly.MainModule.AssemblyReferences
        Write-Verbose "References:`r`n"
        $moduleReferences | Write-Verbose
        $needPatching = $false
        $moduleReferences.ToArray() | Where-Object { $_.FullName -like $referenceFilter } | ForEach-Object {
            $dxReference = $_
            Write-Verbose "`r`nChecking $_ reference...`r`n"
            if ($dxReference.Version -ne $dxVersion) {
                $moduleReferences.Remove($dxReference) | Out-Null
                $newMinor = "$($dxVersion.Major).$($dxVersion.Minor)"
                $newName = [Regex]::Replace($dxReference.Name, ".(v[\d]{2}\.\d)", ".v$newMinor")
                $regex = New-Object Regex("PublicKeyToken=([\w]*)")
                $token = $regex.Match($dxReference).Groups[1].Value
                $regex = New-Object Regex("Culture=([\w]*)")
                $culture = $regex.Match($dxReference).Groups[1].Value
                $newReference = [AssemblyNameReference]::Parse("$newName, Version=$($dxVersion), Culture=$culture, PublicKeyToken=$token")
                $moduleReferences.Add($newreference)
                $moduleAssembly.MainModule.Types | ForEach-Object {
                    $moduleAssembly.MainModule.GetTypeReferences() | Where-Object { $_.Scope -eq $dxReference } | ForEach-Object { 
                        $_.Scope = $newReference 
                    }
                }
                Write-Verbose "$($_.Name) version will changed from $($_.Version) to $($dxVersion)`r`n" 
                $needPatching = $true
            }
            else {
                Write-Verbose "Versions ($($dxReference.Version)) matched nothing to do.`r`n"
            }
        }
        if ($needPatching) {
            Write-Verbose "Patching $modulePath"
            $writeParams = New-Object WriterParameters
            $writeParams.WriteSymbols = $true
            $key = [File]::ReadAllBytes("$PSScriptRoot\Xpand.snk")
            $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
            $pdbPath = Get-Item $modulePath
            $pdbPath = "$($pdbPath.DirectoryName)\$($pdbPath.BaseName).pdb"
            $symbolSources = Get-SymbolSources $pdbPath
            $moduleAssembly.Write($writeParams)
            Update-Symbols -pdb $pdbPath -SymbolSources $symbolSources
        }   
    }
}
function Get-SymbolSources {
    [CmdletBinding()]
    param (
        [parameter(Mandatory, ValueFromPipeline)]
        [System.IO.FileInfo]$pdb,
        [parameter()]
        [string]$dbgToolsPath = "$PSScriptRoot\srcsrv"
    )
    
    begin {
        if (!(test-path $dbgToolsPath)) {
            throw "srcsrv is invalid"
        }
    }
    
    process {
        & "$dbgToolsPath\srctool.exe" $pdb
    }
    
    end {            
    }
}

function Update-Symbols {
    [CmdletBinding()]
    param (
        [parameter(Mandatory, ValueFromPipeline)]
        [System.IO.FileInfo]$pdb,
        [parameter(Mandatory, ParameterSetName = "Default")]
        [string]$TargetRoot,
        [parameter(ParameterSetName = "Default")]
        [string]$SourcesRoot,
        [parameter(ParameterSetName = "Sources")]
        [string[]]$symbolSources,
        [parameter()]
        [string]$dbgToolsPath = "$PSScriptRoot\srcsrv"
    )
    
    begin {
        if (!(test-path $dbgToolsPath)) {
            throw "srcsrv is invalid"
        }
        if ($PSCmdlet.ParameterSetName -eq "Default") {
            $remoteTarget = ($TargetRoot -like "http*")
        }
        else {
            $remoteTarget = $symbolSources | Where-Object { $_ -match "trg: http*" } | Select-Object -First 1
        }
        if (!$remoteTarget ) {
            if (!$SourcesRoot.EndsWith("\")) {
                $SourcesRoot += "\"
            }
            if (!$TargetRoot.EndsWith("\")) {
                $TargetRoot += "\"
            }
        }
        $list = New-Object System.Collections.ArrayList
        $pdbstrPath = "$dbgToolsPath\pdbstr.exe"
    }
    
    process {
        $list.Add($pdb) | Out-Null
    }
    
    end {
        Write-Verbose "Indexing $($list.count) pdb files"
        # $list | Invoke-Parallel -ActivityName Indexing -VariablesToImport @("dbgToolsPath", "TargetRoot", "SourcesRoot", "remoteTarget") -Script {
        $list | foreach {
            Write-Host "Indexing $($_.FullName) ..."
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
            Add-Content -value "SRCSRV: source files ---------------------------------------" -path $streamPath
            if ($symbolSources) {
                $symbolSources | ForEach-Object {
                    $regex = [regex] '(?i)\[([^\]]*)] trg: (.*)'
                    $m = $regex.Match($_)
                    $src = $m.Groups[1].Value
                    $trg = $m.Groups[2].Value
                    if ($src -and $trg) {
                        $result = "$src*$trg";
                        Add-Content -value $result -path $streamPath
                        Write-Verbose "Indexing to $result"
                    }
                }
            }
            else {
                $sources = & "$dbgToolsPath\srctool.exe" -r $_.FullName | Select-Object -SkipLast 1
                if ($sources) {
                    foreach ($src in $sources) {
                        $target = "$src*$TargetRoot$src"
                        if ($remoteTarget) {
                            $file = "$src".replace($SourcesRoot, '').Trim("\").replace("\", "/")
                            $target = "$src*$TargetRoot/$file"
                        }
                        Add-Content -value $target -path $streamPath
                        Write-Verbose "Indexing $src to $target"
                    }
                }
                else {
                    Write-Host "No steppable code in pdb file $_" -f Red
                }       
            }
            Add-Content -value "SRCSRV: end ------------------------------------------------" -path $streamPath
            Write-Verbose "Saving the generated stream into the $_ file..."
            & $pdbstrPath -w -s:srcsrv "-p:$($_.Fullname)" "-i:$streamPath"
            Remove-Item $streamPath
        }
    }
}
