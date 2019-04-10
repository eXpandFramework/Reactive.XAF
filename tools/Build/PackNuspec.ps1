param(
    [parameter(Mandatory)]
    $Branch,
    $nugetBin="$PSScriptRoot\..\..\bin\Nupkg",
    $sourceDir="$PSScriptRoot\..\.."
)
$ErrorActionPreference="Stop"
New-Item $nugetBin -ItemType Directory -Force|Out-Null
$versionConverterSpecPath="$sourceDir\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec"
if ($Branch -match "lab"){
    [xml]$versionConverterSpec=Get-XmlContent $versionConverterSpecPath
    $v=New-Object System.Version($versionConverterSpec.Package.metadata.version)
    if ($v.Revision -eq -1){
        $versionConverterSpec.Package.metadata.version="$($versionConverterSpec.Package.metadata.version).0"
    }
    $versionConverterSpec.Save($versionConverterSpecPath)
    return
}
& (Get-XNugetPath) pack $versionConverterSpecPath -OutputDirectory $nugetBin -NoPackageAnalysis
if ($lastexitcode){
    throw 
}
$packData = [pscustomobject] @{
    nugetBin = $nugetBin
}

set-location $sourceDir
$assemblyVersions=Get-ChildItem "$sourceDir\src" "*.csproj" -Recurse|ForEach-Object{
    $assemblyInfo=get-content "$($_.DirectoryName)\Properties\AssemblyInfo.cs"
    [PSCustomObject]@{
        Name = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
        Version =[System.Text.RegularExpressions.Regex]::Match($assemblyInfo,'Version\("([^"]*)').Groups[1].Value
    }
}

Get-ChildItem "$sourceDir\bin" "*.nuspec" -Recurse|ForEach-Object{
    $name=$_.BaseName.Replace("Xpand.XAF.Modules.","")
    $id="Xpand.XAF.Modules.$name.$name"+"Module"
    Set-Content "$sourceDir\bin\Readme.txt" "BUILD THE PROJECT BEFORE OPENING THE MODEL EDITOR.`r`n`r`nThe package only adds the required references. To install the $id module add the next line in the constructor of your XAF module.`r`n`r`nRequiredModuleTypes.Add(typeof($id));" 
    $packageName=[System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $assembly=$assemblyVersions|Where-Object{$_.name -eq $packageName}
    $name=$_.FullName
    $directory=$_.Directory.Parent.FullName
    & (Get-XNugetPath) pack $name -OutputDirectory $($packData.nugetBin) -Basepath $directory -Version $($assembly.Version)
    if ($lastexitcode){
        throw $_.Exception
    }
}

    
