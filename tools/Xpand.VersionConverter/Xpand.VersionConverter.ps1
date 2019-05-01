using namespace System
using namespace System.Threading
using namespace System.Reflection
using namespace System.IO
using namespace System.IO.Compression
using namespace System.Reflection
using namespace System.Text.RegularExpressions
using namespace Mono.Cecil
using namespace Mono.Cecil.pdb
param(
    [string]$projectFile,
    [string]$targetPath,
    [string]$referenceFilter = "DevExpress*",
    [string]$assemblyFilter = "Xpand.XAF.*"
)

$VerbosePreference = "Continue"
$ErrorActionPreference = "Stop"

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
function Get-MonoAssembly($path, [switch]$Write) {
    $readerParams = New-Object ReaderParameters
    if ($Write) {
        $readerParams.ReadWrite = $true
        $readerParams.SymbolReaderProvider = New-Object PdbReaderProvider
        $readerParams.ReadSymbols = $true
    }
    $readerParams.AssemblyResolver = New-Object MyDefaultAssemblyResolver
    [ModuleDefinition]::ReadModule($path, $readerParams).Assembly
}
function Get-DevExpressVersion($targetPath, $referenceFilter,$dxReferences) {
    Write-Verbose "Finding DevExpress version..."
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
        Use-Object($assembly = Get-MonoAssembly $hintPath) {
            $assembly.name.version
        }
    }
    else {
        $dxAssemblyPath = Get-ChildItem $targetPath "$referenceFilter*.dll" | Select-Object -First 1
        if ($dxAssemblyPath) {
            Write-Verbose "$($dxAssembly.Name.Name) found from $($dxAssemblyPath.FullName)"
            Use-Object($assembly = Get-MonoAssembly $dxAssemblyPath.FullName) {
                $assembly.name.version
            }
        }
        else {
            $include=($dxReferences|Select-Object -First 1).Include
            $dxReference=[Regex]::Match($include, "DevExpress[^,]*", [RegexOptions]::IgnoreCase).Value
            Write-Verbose "Include=$Include"
            Write-Verbose "DxReference=$dxReference"
            $dxAssembly=Get-ChildItem "$env:windir\Microsoft.NET\assembly\GAC_MSIL"  *.dll -Recurse|Where-Object{$_ -like "*$dxReference.dll"}
            if ($dxAssembly){
                Use-Object($assembly = Get-MonoAssembly $dxAssembly.FullName) {
                    $assembly.name.version
                }
            }
            else{
                throw "Cannot find DevExpress Version"
            }
            
        }
    }
}

function Update-Version($modulePath, $dxVersion) {
    Use-Object($moduleAssembly = Get-MonoAssembly $modulePath -Write) {
        $moduleReferences = $moduleAssembly.MainModule.AssemblyReferences
        Write-Verbose "References:"
        $moduleReferences | Write-Verbose
        $needPatching = $false
        $moduleReferences.ToArray() | Where-Object { $_.FullName -like $referenceFilter } | ForEach-Object {
            $dxReference = $_
            Write-Verbose "Checking $_ reference..."
            if ($dxReference.Version -ne $dxVersion) {
                $moduleReferences.Remove($dxReference)|Out-Null
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
                Write-Verbose "$($_.Name) version will changed from $($_.Version) to $($dxVersion)" 
                $needPatching = $true
            }
            else {
                Write-Verbose "Versions ($($dxReference.Version)) matched nothing to do."
            }
        }
        if ($needPatching) {
            Write-Verbose "Patching $modulePath"
            $writeParams = New-Object WriterParameters
            $writeParams.WriteSymbols = $true
            $key = [byte[]]::new(0)
            $key = [File]::ReadAllBytes("$root\Xpand.snk")
            $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
            $moduleAssembly.Write($writeParams)
        }   
    }
}
try {
    $mtx = [Mutex]::OpenExisting("VersionConverterMutex")
}
catch {
    $mtx = [Mutex]::new($true, "VersionConverterMutex")
}

try {
    set-location $targetPath
    Write-Verbose "Running Version Converter on project $projectFile with target $targetPath"
    $projectFileInfo = Get-Item $projectFile
    [xml]$csproj = Get-Content $projectFileInfo.FullName
    $references = $csproj.Project.ItemGroup.Reference
    $dxReferences = $references | Where-Object { $_.Include -like "$referenceFilter" }
    $root = $PSScriptRoot
    Write-Verbose "Loading Mono.Cecil"
    $monoPath = "$root\mono.cecil.0.10.3\lib\net40"
    if (!(Test-Path "$monoPath\Mono.Cecil.dll")) {
        $client = New-Object System.Net.WebClient
        $client.DownloadFile("https://www.nuget.org/api/v2/package/Mono.Cecil/0.10.3", "$root\mono.cecil.0.10.3.zip")
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [ZipFile]::ExtractToDirectory("$root\mono.cecil.0.10.3.zip", "$root\mono.cecil.0.10.3")
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
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(string.Format(@"$targetPath\{0}.dll", name.Name));
            return assemblyDefinition;
        }
    }
}
"@ -ReferencedAssemblies @("$monoPath\Mono.Cecil.dll")

    $dxVersion = Get-DevExpressVersion $targetPath $referenceFilter $dxReferences|Where-Object{$_}|Select-Object -First 1
    $references | Where-Object { $_.Include -like $assemblyFilter } | ForEach-Object {
        "$targetPath\$([Path]::GetFileName($_.HintPath))", "$($projectFileInfo.DirectoryName)\$($_.HintPath)" | ForEach-Object {
            if (Test-Path $_) {
                $modulePath = (Get-Item $_).FullName
                Write-Verbose "Checking $modulePath references.."
                Update-Version $modulePath $dxVersion
            }
        }
    }
}
catch {
    throw $_.Exception
}
finally {
    $mtx.ReleaseMutex()
    $mtx.Dispose()
}
