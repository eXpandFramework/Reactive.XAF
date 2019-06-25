function Get-UnPatchedPackages {
    param(
        $moduleDirectories,
        $dxVersion
    )
    $unpatchedPackages = $moduleDirectories | ForEach-Object {
        Get-ChildItem $_ "Xpand.XAF*.dll" -Recurse | ForEach-Object {
            if (!(Test-Path "$($_.DirectoryName)\VersionConverter.v.$dxVersion.DoNotDelete")) {
                $_.fullname
            }
        }
    }
    Write-Verbose "unpatchedPackages:"
    $unpatchedPackages | Write-Verbose
    $unpatchedPackages
}
function Get-InstalledPackages {
    param(
        $projectFile,
        $assemblyFilter
    )
    [xml]$csproj = get-content $projectFile
    $packagesFolder = Get-packagesfolder
    
    [array]$packageReferences = $csproj.Project.ItemGroup.PackageReference | ForEach-Object {
        if ($_.Include -like "$assemblyFilter") {
            [PSCustomObject]@{
                Id      = $_.Include
                Version = $_.Version
            }
        }
    }
    
    [array]$dependencies = $packageReferences | ForEach-Object { Get-PackageDependencies $_ $packagesFolder $assemblyFilter $projectFile }
    $dependencies + $packageReferences
}

function Get-PackageDependencies {
    [CmdletBinding()]
    param (
        [parameter(ValueFromPipeline)]
        $psObj,
        $packagesFolder,
        $assemblyFilter,
        $projectFile
    )
    
    begin {
    }
    
    process {
        $nuspecPath = "$packagesFolder\$($psObj.Id)\$($psObj.Version)\$($psObj.Id).nuspec"
        if (!(Test-Path $nuspecPath)) {
            Restore-Packages $projectFile
            if (!(Test-Path $nuspecPath)) {
                throw "$nuspecPath not found."
            }
        }
        [xml]$nuspec = Get-Content $nuspecPath
        
        [array]$packages = $nuspec.package.metadata.dependencies.group.dependency | Where-Object { $_.id -like "$assemblyFilter" } | ForEach-Object {
            [PSCustomObject]@{
                Id      = $_.Id
                Version = $_.Version
            }
        } 
        
        [array]$dependencies = $packages | ForEach-Object { Get-PackageDependencies $_ $packagesFolder $assemblyFilter $projectFile }
        $dependencies + $packages
        
    }

    end {
    }
}

function Restore-Packages {
    $nuget = "$PSScriptRoot\nuget.exe"
    if (!(Test-Path $nuget)) {
        $c = [System.Net.WebClient]::new()
        $c.DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", $nuget)
        $c.dispose()
    }
    & $nuget Restore $projectFile | Out-Null
}
function Get-PackagesFolder {
    $packagesFolder = "$PSSCriptRoot\..\..\.."
    if ((Get-Item "$PSScriptRoot\..").BaseName -like "Xpand.VersionConverter*") {
        $packagesFolder = "$PSSCriptRoot\..\.."
    }
    $packagesFolder
}

function Install-MonoCecil($resolvePath) {
    Write-Verbose "Loading Mono.Cecil"
    $monoPath = "$PSScriptRoot\mono.cecil.0.10.4\lib\net40"
    if (!(Test-Path "$monoPath\Mono.Cecil.dll")) {
        $client = New-Object System.Net.WebClient
        $client.DownloadFile("https://www.nuget.org/api/v2/package/Mono.Cecil/0.10.4", "$PSScriptRoot\mono.cecil.0.10.4.zip")
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [ZipFile]::ExtractToDirectory("$PSScriptRoot\mono.cecil.0.10.4.zip", "$PSScriptRoot\mono.cecil.0.10.4")
    }

    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.dll")) | Out-Null
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.pdb.dll")) | Out-Null
    $packagesFolder = Get-PackagesFolder 
    Add-Type @"
    using System;
    using System.Diagnostics;
    using Mono.Cecil;
    using System.IO;
    
    public class MyDefaultAssemblyResolver : DefaultAssemblyResolver{
        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters){
            try{
                return base.Resolve(name, parameters);
            }
            catch (AssemblyResolutionException){
                var assemblies = Directory.GetFiles(@"$packagesFolder", string.Format("{0}.dll", name.Name),SearchOption.AllDirectories);
                foreach (var assembly in assemblies){
                    var fileVersion = new Version(FileVersionInfo.GetVersionInfo(assembly).FileVersion);
                    if (fileVersion == name.Version){
                        return AssemblyDefinition.ReadAssembly(assembly);
                    }
                }
                return AssemblyDefinition.ReadAssembly(string.Format(@"$resolvePath\{0}.dll", name.Name));
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
function Get-MonoAssembly($path, [switch]$ReadSymbols) {
    $readerParams = New-Object ReaderParameters
    $readerParams.ReadWrite = $true
    $readerParams.ReadSymbols = $ReadSymbols
    $readerParams.AssemblyResolver = New-Object MyDefaultAssemblyResolver
    try {
        $m = [ModuleDefinition]::ReadModule($path, $readerParams)
        $m.Assembly
    }
    catch {
        if ($_.FullyQualifiedErrorId -like "*Symbols*") {
            Get-MonoAssembly $path
        }
        else {
            Write-Warning "$($_.FullyQualifiedErrorId) exception when loading $path"
            throw $_.Exception
        }
    }
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
    Use-Object($moduleAssembly = Get-MonoAssembly $modulePath -ReadSymbols) {
        $moduleReferences = $moduleAssembly.MainModule.AssemblyReferences
        Write-Verbose "References:`r`n"
        $moduleReferences | Write-Verbose
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
                Write-Verbose "$($_.Name) version will changed from $($_.Version.Major) to $($dxVersion)`r`n" 
                Write-Verbose "Patching $modulePath"
                $writeParams = New-Object WriterParameters
                $writeParams.WriteSymbols = $moduleAssembly.MainModule.hassymbols
                $key = [File]::ReadAllBytes("$PSScriptRoot\Xpand.snk")
                $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
                $moduleAssembly.Write($writeParams)
                if ($writeParams.WriteSymbols) {
                    $pdbPath = Get-Item $modulePath
                    $pdbPath = "$($pdbPath.DirectoryName)\$($pdbPath.BaseName).pdb"
                    $symbolSources = Get-SymbolSources $pdbPath
                    Update-Symbols -pdb $pdbPath -SymbolSources $symbolSources
                }
                break
            }
            else {
                Write-Verbose "Versions ($($dxReference.Version)) matched nothing to do.`r`n"
            }
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
