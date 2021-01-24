. "$PSScriptRoot\Write-HostFormatted.ps1"
. "$PSScriptRoot\ConvertTo-FramedText.ps1"
. "$PSScriptRoot\Write-HostFormatted.ps1"
. "$PSScriptRoot\ConvertTo-FramedText.ps1"
. "$PSScriptRoot\Get-DevExpressVersion.ps1"
$global:howToVerbose

function ConfigureVerbose {
    param (
        $VerboseOutput,
        $AttributeName
    )
    $global:howToVerbose = "Edit $projectFile and enable verbose messaging by adding <PropertyGroup><$AttributeName>Continue</$AttributeName>, alternatively create an Enviroment variable named $AttributeName and set its value to 1. Rebuild the project and send the $PSScriptRoot\execution.log to support."
    if ( [System.Environment]::GetEnvironmentVariable($AttributeName) -eq 1) {
        $VerboseOutput = "continue"
    }
    $VerboseOutput
}

function Write-VerboseLog {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory , ValueFromPipeline )]
        [AllowEmptyString()]
        [string]
        $Message
    )

    process {
        if ($VerbosePreference -eq "continue") {
            Write-Verbose $Message -Verbose
            $fs=[System.IO.File]::Open("$PSScriptRoot\execution.log",[System.IO.FileMode]::OpenOrCreate,[System.IO.FileAccess]::ReadWrite,[System.IO.FileShare]::ReadWrite)
            $sw=[System.IO.StreamWriter]::new($fs)
            $sw.WriteLine($Message)
            $sw.Dispose()
            $fs.Dispose()
        }
    }
    
    
}

function Install-MonoCecil($resolvePath) {
    Write-VerboseLog "Loading Mono.Cecil"
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
    $resolver=[Mono.Cecil.DefaultAssemblyResolver]::new()
    $readerParams.AssemblyResolver = $resolver
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
function Test-Version {
    param (
        [parameter(Mandatory,ValueFromPipeline)]
        [string]$Version
    )
    
    begin {
    
    }
    
    process {
        [version]$v=$null
        [version]::TryParse($Version,[ref]$v)
    }
    end {
        
    }
}
function Switch-DependencyVersion() {
    param(
        [string]$modulePath, 
        [version]$Version,
        [string]$referenceFilter,
        [string]$snkFile,
        [System.IO.FileInfo[]]$assemblyList
    )
    
    $moduleAssemblyData = Get-MonoAssembly $modulePath $r -ReadSymbols
    $moduleAssembly = $moduleAssemblyData.assembly
    $switchedRefs = Switch-AssemblyNameReferences $moduleAssembly $referenceFilter $version
    if ($switchedRefs) {
        Switch-TypeReferences $moduleAssembly $referenceFilter $switchedRefs
        Switch-Attributeparameters $moduleAssembly $referenceFilter $switchedRefs
        Write-Assembly $modulePath $moduleAssembly $snkFile $Version
    }
    $moduleAssembly.Dispose()
}

function New-AssemblyReference {
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

function Write-Assembly {
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

function Switch-TypeReferences {
    param(
        $moduleAssembly,
        $referenceFilter,
        [hashtable]$AssemblyReferences
    )
    $typeReferences = $moduleAssembly.MainModule.GetTypeReferences()
    $moduleAssembly.MainModule.Types | ForEach-Object {
        $typeReferences | Where-Object { $_.Scope -like $referenceFilter } | ForEach-Object { 
            $scope = $AssemblyReferences[$_.Scope.FullName]
            if ($scope) {
                $_.Scope = $scope
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
    @(($moduleAssembly.MainModule.Types.CustomAttributes.ConstructorArguments | Where-Object {
                $_.Type.FullName -eq "System.Type" -and $_.Value.Scope -like $referenceFilter
            }).Value) | Where-Object { $_ } | ForEach-Object {
        $scope = $AssemblyReferences[$_.Scope.FullName]
        if ($scope) {
            $_.Scope = $scope
        }
    }
}
function Switch-AssemblyNameReferences {
    param (
        $moduleAssembly,
        $referenceFilter,
        $Version
    )
    $moduleReferences = $moduleAssembly.MainModule.AssemblyReferences
    $refs = $moduleReferences.ToArray() | Where-Object { $_.Name -like $referenceFilter } | ForEach-Object {
        if ($_.Version -ne $Version) {
            $moduleReferences.Remove($_) | Out-Null
            $assembNameReference = (New-AssemblyReference $_ $version)
            $moduleReferences.Add($assembNameReference)
            @{
                Old = $_.FullName
                New = $assembNameReference
            }
        }
    }
    $refs | ConvertToDictionary -KeyPropertyName Old -ValuePropertyName New
}

function ConvertToDictionary {
    [CmdletBinding()]
    param (
        [Parameter(Position = 0, Mandatory, ValueFromPipeline)] 
        [object] $Object ,
        [parameter(Mandatory, Position = 1)]
        [string]$KeyPropertyName,
        [string]$ValuePropertyName,
        # [parameter(Mandatory)]
        [scriptblock]$ValueSelector,
        [switch]$Force
    )
    
    begin {
        $output = @{ }
    }
    
    process {
        $key = $Object.($KeyPropertyName)
        if (!$Force) {
            if (!$output.ContainsKey($key)) {
                if ($ValueSelector) {
                    $output.add($key, (& $ValueSelector $Object))
                }
                else {
                    $value = $Object.($ValuePropertyName)
                    $output.add($key, $value)
                }
                
            }
        }
        else {
            $output.add($key, (& $ValueSelector $Object))
        }
        
    }
    
    end {
        $output
    }
}


function Install-MonoCecil($resolvePath) {
    Write-VerboseLog "Loading Mono.Cecil"
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

function Get-MonoAssembly($path, $resolver, [switch]$ReadSymbols) {
    $readerParams = New-Object ReaderParameters
    $readerParams.ReadWrite = $true
    $readerParams.AssemblyResolver = $resolver
    try {
        $m = [ModuleDefinition]::ReadModule($path, $readerParams)
        [PSCustomObject]@{
            Assembly = $m.assembly
            Resolver = $r
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
    $switchedRefs = Switch-AssemblyNameReferences $moduleAssembly $referenceFilter $version
    if ($switchedRefs) {
        Switch-TypeReferences $moduleAssembly $referenceFilter $switchedRefs
        Switch-Attributeparameters $moduleAssembly $referenceFilter $switchedRefs
        Write-Assembly $modulePath $moduleAssembly $snkFile $Version
    }
    $moduleAssemblyData.Resolver.Dispose()
    $moduleAssembly.Dispose()
}

function New-AssemblyReference {
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

function Write-Assembly {
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

function Switch-TypeReferences {
    param(
        $moduleAssembly,
        $referenceFilter,
        [hashtable]$AssemblyReferences
    )
    $typeReferences = $moduleAssembly.MainModule.GetTypeReferences()
    $moduleAssembly.MainModule.Types | ForEach-Object {
        $typeReferences | Where-Object { $_.Scope -like $referenceFilter } | ForEach-Object { 
            $scope = $AssemblyReferences[$_.Scope.FullName]
            if ($scope) {
                $_.Scope = $scope
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
    @(($moduleAssembly.MainModule.Types.CustomAttributes.ConstructorArguments | Where-Object {
                $_.Type.FullName -eq "System.Type" -and $_.Value.Scope -like $referenceFilter
            }).Value) | Where-Object { $_ } | ForEach-Object {
        $scope = $AssemblyReferences[$_.Scope.FullName]
        if ($scope) {
            $_.Scope = $scope
        }
    }
}
function Switch-AssemblyNameReferences {
    param (
        $moduleAssembly,
        $referenceFilter,
        $Version
    )
    $moduleReferences = $moduleAssembly.MainModule.AssemblyReferences
    $refs = $moduleReferences.ToArray() | Where-Object { $_.Name -like $referenceFilter } | ForEach-Object {
        if ($_.Version -ne $Version) {
            $moduleReferences.Remove($_) | Out-Null
            $assembNameReference = (New-AssemblyReference $_ $version)
            $moduleReferences.Add($assembNameReference)
            @{
                Old = $_.FullName
                New = $assembNameReference
            }
        }
    }
    $refs | ConvertToDictionary -KeyPropertyName Old -ValuePropertyName New
}

function ConvertToDictionary {
    [CmdletBinding()]
    param (
        [Parameter(Position = 0, Mandatory, ValueFromPipeline)] 
        [object] $Object ,
        [parameter(Mandatory, Position = 1)]
        [string]$KeyPropertyName,
        [string]$ValuePropertyName,
        # [parameter(Mandatory)]
        [scriptblock]$ValueSelector,
        [switch]$Force
    )
    
    begin {
        $output = @{ }
    }
    
    process {
        $key = $Object.($KeyPropertyName)
        if (!$Force) {
            if (!$output.ContainsKey($key)) {
                if ($ValueSelector) {
                    $output.add($key, (& $ValueSelector $Object))
                }
                else {
                    $value = $Object.($ValuePropertyName)
                    $output.add($key, $value)
                }
                
            }
        }
        else {
            $output.add($key, (& $ValueSelector $Object))
        }
        
    }
    
    end {
        $output
    }
}
