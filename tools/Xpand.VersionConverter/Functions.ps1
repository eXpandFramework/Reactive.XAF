. .\Write-HostFormatted.ps1
function Get-UnPatchedPackages {
    param(
        $moduleDirectories,
        $dxVersion
    )
    $unpatchedPackages = $moduleDirectories | ForEach-Object {
        (@(Get-ChildItem $_ "Xpand.XAF*.dll" -Recurse ) + @(Get-ChildItem $_ "Xpand.Extensions*.dll" -Recurse )) | ForEach-Object {
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
    [xml]$csproj = Get-Content $projectFile
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
    $monoPath = "$PSScriptRoot\mono.cecil"
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.dll")) | Out-Null
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.pdb.dll")) | Out-Null
    $packagesFolder = Get-PackagesFolder 
    Add-Type @"
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Mono.Cecil;

    public class MyDefaultAssemblyResolver : DefaultAssemblyResolver{
        List<AssemblyDefinition> _resolvedDefinitions=new List<AssemblyDefinition>();
        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters){
            var definition = ResolveAssemblyDefinition(name, parameters);
            _resolvedDefinitions.Add(definition);
            return definition;
        }

        private AssemblyDefinition ResolveAssemblyDefinition(AssemblyNameReference name, ReaderParameters parameters){
            try{
                return base.Resolve(name, parameters);
            }
            catch (AssemblyResolutionException){
                return AssemblyDefinition(name);
            }
        }

        protected override void Dispose(bool disposing){
            base.Dispose(disposing);
            foreach (var resolvedDefinition in _resolvedDefinitions){
                resolvedDefinition.Dispose();
            }
        }

        private static AssemblyDefinition AssemblyDefinition(AssemblyNameReference name){
            var assemblies = Directory.GetFiles(@"$packagesFolder", string.Format("{0}.dll", name.Name),
                SearchOption.AllDirectories);
            foreach (var assembly in assemblies){
                var fileVersion = new Version(FileVersionInfo.GetVersionInfo(assembly).FileVersion);
                if (fileVersion == name.Version){
                    return Mono.Cecil.AssemblyDefinition.ReadAssembly(assembly);
                }
            }

            return Mono.Cecil.AssemblyDefinition.ReadAssembly(string.Format(@"$resolvePath\{0}.dll", name.Name));
        }
    }
"@ -ReferencedAssemblies @("$monoPath\Mono.Cecil.dll")
}
function Remove-PatchFlags {
    param(
        $PackageDir,
        $DXVersion
    )
    Get-ChildItem $packageDir *VersionConverter.v.* | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
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
    $assemblyResolver = New-Object MyDefaultAssemblyResolver
    $readerParams.AssemblyResolver = $assemblyResolver
    try {
        $m = [ModuleDefinition]::ReadModule($path, $readerParams)
        [PSCustomObject]@{
            Assembly = $m.assembly
            Resolver = $assemblyResolver
        }
    }
    catch {
        if ($_.FullyQualifiedErrorId -like "*Symbols*" -or ($_.FullyQualifiedErrorId -like "pdbex*")) {
            Get-MonoAssembly $path
        }
        else {
            Write-Warning "$($_.FullyQualifiedErrorId) exception when loading $path"
            throw $_.Exception
        }
    }
}

function Get-PaketReferences {
    [CmdletBinding()]
    param (
        [System.IO.FileInfo]$projectFile = "."
    )
    
    begin {
        
    }
    
    process {
        $paketDirectoryInfo = $projectFile.Directory
        $paketReferencesFile = "$($paketDirectoryInfo.FullName)\paket.references"
        if (Test-Path $paketReferencesFile) {
            Push-Location $projectFile.DirectoryName
            $dependencies=dotnet paket show-installed-packages --project $projectFile.FullName --all --silent |ForEach-Object {
                $parts = $_.split(" ")
                [PSCustomObject]@{
                    Include      = $parts[1]
                    Version = $parts[3]
                }
            }
            Pop-Location
            $c=Get-Content $paketReferencesFile|ForEach-Object{
                $ref=$_
                $d=$dependencies|Where-Object{
                    $ref-eq $_.Include
                }
                $d
            }
            Write-Output $c
        }
    }
    
    end {
        
    }
}

function Get-PaketDependenciesPath {
    [CmdletBinding()]
    param (
        [string]$Path="."
    )
    
    begin {
        
    }
    
    process {
        $paketDirectoryInfo = (Get-Item $Path).Directory
        if (!$paketDirectoryInfo){
            $paketDirectoryInfo = Get-Item $Path
        }
        $paketDependeciesFile = "$($paketDirectoryInfo.FullName)\paket.dependencies"
        while (!(Test-Path $paketDependeciesFile)) {
            $paketDirectoryInfo = $paketDirectoryInfo.Parent
            if (!$paketDirectoryInfo){
                return
            }
            $paketDependeciesFile = "$($paketDirectoryInfo.FullName)\paket.dependencies"
        }
        $item=Get-Item $paketDependeciesFile
        Set-Location $item.Directory.Parent.FullName
        $item
    }
    
    end {
        
    }
}
function Get-DevExpressVersion($targetPath, $referenceFilter, $projectFile) {
    try {
        Write-Verbose "Locating DevExpress version..."
        $projectFileInfo = Get-Item $projectFile
        [xml]$csproj = Get-Content $projectFileInfo.FullName
        $packageReference = $csproj.Project.ItemGroup.PackageReference |Where-Object{$_}
        if (!$packageReference){
            $packageReference=Get-PaketReferences (Get-Item $projectFile)
        }
        $packageReference=$packageReference| Where-Object { $_.Include -like "$referenceFilter" }
        if ($packageReference) {
            $v = ($packageReference ).Version | Select-Object -First 1
            if ($packageReference) {
                $version = [version]$v
            }
        }
        
        if (!$packageReference -and !$paket){
            $references = $csproj.Project.ItemGroup.Reference
            $dxReferences = $references.Include | Where-Object { $_ -like "$referenceFilter" }    
            $hintPath = $dxReferences.HintPath | ForEach-Object { 
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
        "Exception:"
        $_.Exception
        "InvocationInfo:"
        $_.InvocationInfo 
        Write-Warning "$howToVerbose`r`n"
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
    $hintPath = $dxReferences.HintPath | ForEach-Object { 
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
function Write-Intent {
    [CmdletBinding()]
    param (
        [parameter(ValueFromPipeline)]
        [string]$Text,
        [int]$Level
    )
    
    begin {
        
    }
    
    process {
        for ($i = 0; $i -lt $Level.Count; $i++) {
            $prefix+="  "    
        }
        $prefix+=$text
        Write-Output $prefix       
    }
    
    end {
        
    }
}
function Update-Version($modulePath, $dxVersion,$referenceFilter,$snkFile) {
    $moduleAssemblyData = Get-MonoAssembly $modulePath -ReadSymbols
    $moduleAssembly = $moduleAssemblyData.assembly
    $moduleReferences = $moduleAssembly.MainModule.AssemblyReferences
    "References:`r`n"
    $moduleReferences.Fullname |Sort-Object
    $needsPatching = $false
    $moduleReferences.ToArray() | Where-Object { $_.Name -like $referenceFilter } | ForEach-Object {
        $dxReference = $_
        "Checking reference $_..."
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
            Write-HostFormatted "$($_.Name) version will changed from $($_.Version) to $($dxVersion)`r`n" -ForegroundColor Blue
            $needsPatching = $true
        }
        else {
            Write-HostFormatted "$($_.Name) Version ($($dxReference.Version)) matched nothing to do.`r`n" -ForegroundColor Blue
        }
    }
    if ($needsPatching) {
        "Patching $modulePath"
        $writeParams = New-Object WriterParameters
        $writeParams.WriteSymbols = $moduleAssembly.MainModule.hassymbols
        $key = [File]::ReadAllBytes($snkFile)
        $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
        if ($writeParams.WriteSymbols) {
            $pdbPath = Get-Item $modulePath
            $pdbPath = "$($pdbPath.DirectoryName)\$($pdbPath.BaseName).pdb"
            $symbolSources = Get-SymbolSources $pdbPath
        }
        $moduleAssembly.Write($writeParams)
        "Patched $modulePath"
        if ($writeParams.WriteSymbols) {
            "Symbols $modulePath"
            if ($symbolSources -notmatch "is not source indexed") {
                Update-Symbols -pdb $pdbPath -SymbolSources $symbolSources
            }
            else {
                $symbolSources 
            }
        }
    }
    $moduleAssemblyData.Resolver.Dispose()
    $moduleAssembly.Dispose()
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
        if (!(Test-Path $dbgToolsPath)) {
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
        if (!(Test-Path $dbgToolsPath)) {
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
