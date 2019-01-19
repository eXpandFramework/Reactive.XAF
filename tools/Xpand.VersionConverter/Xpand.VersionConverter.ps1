param(
    [parameter(Mandatory)]
    [string]$projectFile = $null,
    [parameter(Mandatory)]
    $targetPath = $null,
    $referenceFilter = "DevExpress*",
    $assemblyFilter = "Xpand.XAF.*"
)
$ErrorActionPreference = "Stop"
$projectFileInfo = Get-item $projectFile
[xml]$csproj = Get-Content $projectFileInfo.FullName
$references = $csproj.Project.ItemGroup.Reference
$dxReferences = $references|Where-Object {$_.Include -like "$referenceFilter"}
$root = $PSScriptRoot
Invoke-Command {
    "Loading Mono.Cecil"
    $monoPath = "$root\mono.cecil.0.10.1\lib\net40\Mono.Cecil.dll"
    if (!(Test-Path $monoPath)) {
        $client = New-Object System.Net.WebClient
        $client.DownloadFile("https://www.nuget.org/api/v2/package/Mono.Cecil/0.10.1", "$root\mono.cecil.0.10.1.zip")
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [System.IO.Compression.ZipFile]::ExtractToDirectory("$root\mono.cecil.0.10.1.zip", "$root\mono.cecil.0.10.1")
    }
    $bytes = [System.IO.File]::ReadAllBytes($monoPath)
    [System.Reflection.Assembly]::Load($bytes)|out-null
}
$sourceAssemblyName = Invoke-Command {
    "Finding DX assembly name"
    $dxAssemblyPath = (Get-ChildItem $targetPath "$referenceFilter*.dll" |Select-Object -First 1).FullName
    if ($dxAssemblyPath) {
        $dxAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dxAssemblyPath)
        "$sourceAssemblyName found from $dxAssemblyPath"
        $dxAssembly.Name
    }
    else {
        $name = ($dxReferences|Where-Object {$_.Include -like "*Version*"}|Select-Object -First 1).Include
        new-object System.Reflection.AssemblyName($name)
    }
}|Select-Object -last 1
if (!$sourceAssemblyName) {
    throw "Cannot find $referenceFilter version in $($projectFileInfo.Name)"
}
$references|Where-Object {$_.Include -like $assemblyFilter}|ForEach-Object {
    "$targetPath\$([System.IO.Path]::GetFileName($_.HintPath))", "$($projectFileInfo.DirectoryName)\$($_.HintPath)"|ForEach-Object {
        $modulePath=(Get-Item $_).FullName
        $readerParams = new-object Mono.Cecil.ReaderParameters
        $readerParams.ReadWrite = $true
        $moduleAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($modulePath, $readerParams)
        Write-host "Checking $modulePath references.." -f "Blue"
        $moduleAssembly.MainModule.AssemblyReferences.ToArray()|Where-Object {$_.FullName -like $referenceFilter}|ForEach-Object {
            $nowReference = $_
            if ($nowReference.Version -ne $sourceAssemblyName.Version) {
                $moduleAssembly.MainModule.AssemblyReferences.Remove($nowReference)
                $newMinor = "$($sourceAssemblyName.Version.Major).$($sourceAssemblyName.Version.Minor)"
                $newName = [System.Text.RegularExpressions.Regex]::Replace($nowReference.Name, ".(v[\d]{2}\.\d)", ".v$newMinor")
                $regex = New-Object System.Text.RegularExpressions.Regex("PublicKeyToken=([\w]*)")
                $token = $regex.Match($nowReference).Groups[1].Value
                $regex = New-Object System.Text.RegularExpressions.Regex("Culture=([\w]*)")
                $culture = $regex.Match($nowReference).Groups[1].Value
                $newReference = [Mono.Cecil.AssemblyNameReference]::Parse("$newName, Version=$($sourceAssemblyName.Version), Culture=$culture, PublicKeyToken=$token")
                $moduleAssembly.MainModule.AssemblyReferences.Add($newreference)
                $moduleAssembly.MainModule.Types|
                    ForEach-Object {$moduleAssembly.MainModule.GetTypeReferences()| Where-Object {$_.Scope -eq $nowReference}|
                        ForEach-Object {$_.Scope = $newReference}}
                Write-Host "$($_.Name) version changed from $($_.Version) to $($sourceAssemblyName.Version)" -f Green
            }
        }
        $moduleAssembly.Write()
        $moduleAssembly.Dispose()
    }
}
