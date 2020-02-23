. "$PSScriptRoot\Write-HostFormatted.ps1"
. "$PSScriptRoot\ConvertTo-FramedText.ps1"
function GetDevExpressVersion($targetPath, $referenceFilter, $projectFile) {
    try {
        Write-Verbose "Locating DevExpress version..."
        $projectFileInfo = Get-Item $projectFile
        [xml]$csproj = Get-Content $projectFileInfo.FullName
        $packageReference = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_ }
        if (!$packageReference) {
            $packageReference = Get-PaketReferences (Get-Item $projectFile)
        }
        $packageReference = $packageReference | Where-Object { $_.Include -like "$referenceFilter" }
        if ($packageReference) {
            $v = ($packageReference ).Version | Select-Object -First 1
            if ($packageReference) {
                $version = [version]$v
                if ($version.Revision -eq -1) {
                    $v += ".0"
                    $version = [version]$v
                }
            }
        }
        
        if (!$packageReference -and !$paket) {
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

function Install-MonoCecil($resolvePath) {
    Write-Verbose "Loading Mono.Cecil"
    $monoPath = "$PSScriptRoot\mono.cecil"
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.dll")) | Out-Null
    [System.Reflection.Assembly]::Load([File]::ReadAllBytes("$monoPath\Mono.Cecil.pdb.dll")) | Out-Null
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
                Stop-Process -id $pid
            }
        }
    }
}

function Get-MonoAssembly($path, $AssemblyList, [switch]$ReadSymbols) {
    $readerParams = New-Object ReaderParameters
    $readerParams.ReadWrite = $true
    $readerParams.ReadSymbols = $ReadSymbols
    . "$PSScriptRoot\AssemblyResolver.ps1"
    $assemblyResolver = [AssemblyResolver]::new($assemblyList)
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
            Get-MonoAssembly $path $assemblyList
        }
        else {
            Write-Warning "$($_.FullyQualifiedErrorId) exception when loading $path"
            throw $_.Exception
        }
    }
}

function Switch-AssemblyDependencyVersion() {
    param(
        [string]$modulePath, 
        [version]$Version,
        [string]$referenceFilter,
        [string]$snkFile,
        [System.IO.FileInfo[]]$assemblyList
    )
    $moduleAssemblyData = Get-MonoAssembly $modulePath $assemblyList -ReadSymbols
    $moduleAssembly = $moduleAssemblyData.assembly
    $switchedRefs=Switch-AssemblyNameReferences $moduleAssembly $referenceFilter $version
    if ($switchedRefs) {
        Switch-TypeReferences $moduleAssembly $referenceFilter $switchedRefs
        Switch-Attributeparameters $moduleAssembly $referenceFilter $switchedRefs
        Write-Assembly $modulePath $moduleAssembly $snkFile $Version
    }
    $moduleAssemblyData.Resolver.Dispose()
    $moduleAssembly.Dispose()
}

function New-AssemblyReference{
    param(
        [Mono.Cecil.AssemblyNameReference]$AsemblyNameReference,
        $Version
    )
    $newMinor = "$($Version.Major).$($Version.Minor)"
    $newName = [Regex]::Replace($AsemblyNameReference.Name, "\.v\d{2}\.\d", ".v$newMinor")
    $regex = New-Object Regex("PublicKeyToken=([\w]*)") 
    $token = $regex.Match($AsemblyNameReference).Groups[1].Value
    $regex = New-Object Regex("Culture=([\w]*)")
    $culture = $regex.Match($AsemblyNameReference).Groups[1].Value
    [AssemblyNameReference]::Parse("$newName, Version=$($Version), Culture=$culture, PublicKeyToken=$token")
}

function Write-Assembly{
    param(
        $modulePath,
        $moduleAssembly,
        $snkFile,
        $version
    )
    
    wh "Switching $($moduleAssembly.Name.Name) to $version" -ForegroundColor Yellow
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
    wh "Patched $modulePath" -ForegroundColor Green
    if ($writeParams.WriteSymbols) {
        "Symbols $modulePath"
        if ($symbolSources -notmatch "is not source indexed") {
            Update-Symbols -pdb $pdbPath -SymbolSources $symbolSources
        }
        else {
            $global:lastexitcode = 0
            $symbolSources 
        }
    }
}

function Switch-TypeReferences{
    param(
        $moduleAssembly,
        $referenceFilter,
        [hashtable]$AssemblyReferences
    )
    $typeReferences = $moduleAssembly.MainModule.GetTypeReferences()
    $moduleAssembly.MainModule.Types | ForEach-Object {
        $typeReferences | Where-Object { $_.Scope -like $referenceFilter } | ForEach-Object { 
            $scope=$AssemblyReferences[$_.Scope.FullName]
            if ($scope){
                $_.Scope=$scope
            }
        }
    }
}

function Switch-AttributeParameters {
    param (
        $moduleAssembly,
        $referenceFilter,
        [hashtable]$AssemblyReferences
    )
    @(($moduleAssembly.MainModule.Types.CustomAttributes.ConstructorArguments |Where-Object{
        $_.Type.FullName -eq "System.Type" -and $_.Value.Scope -like $referenceFilter
    }).Value)|Where-Object{$_}|ForEach-Object{
        $scope=$AssemblyReferences[$_.Scope.FullName]
        if ($scope){
            $_.Scope=$scope
        }
    }
}
function Switch-AssemblyNameReferences {
    param (
        $moduleAssembly,
        $referenceFilter,
        $Version
    )
    $moduleReferences=$moduleAssembly.MainModule.AssemblyReferences
    $refs=$moduleReferences.ToArray() | Where-Object { $_.Name -like $referenceFilter } | ForEach-Object {
        if ($_.Version -ne $Version) {
            $moduleReferences.Remove($_) | Out-Null
            $assembNameReference=(New-AssemblyReference $_ $version)
            $moduleReferences.Add($assembNameReference)
            @{
                Old = $_.FullName
                New=$assembNameReference
            }
        }
    }
    $refs|ConvertToDictionary -KeyPropertyName Old -ValuePropertyName New
}

function ConvertToDictionary {
    [CmdletBinding()]
    param (
        [Parameter(Position = 0,Mandatory,ValueFromPipeline)] 
        [object] $Object ,
        [parameter(Mandatory,Position=1)]
        [string]$KeyPropertyName,
        [string]$ValuePropertyName,
        # [parameter(Mandatory)]
        [scriptblock]$ValueSelector,
        [switch]$Force
    )
    
    begin {
        $output = @{}
    }
    
    process {
        $key=$Object.($KeyPropertyName)
        if (!$Force){
            if (!$output.ContainsKey($key)){
                if ($ValueSelector){
                    $output.add($key,(& $ValueSelector $Object))
                }
                else{
                    $value=$Object.($ValuePropertyName)
                    $output.add($key,$value)
                }
                
            }
        }
        else{
            $output.add($key,(& $ValueSelector $Object))
        }
        
    }
    
    end {
        $output
    }
}
