param(

    $Branch="master",
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
    $message=@"
    
    The package only adds the required references. To install the $id module add the next line in the constructor of your XAF module.
    
    RequiredModuleTypes.Add(typeof($id));

    BUILD THE PROJECT BEFORE OPENING THE MODEL EDITOR
    
    To read the module documentation visit the wiki page @ https://github.com/eXpandFramework/DevExpress.XAF/wiki/$name"

    if you like our work please consider to give us a star https://github.com/eXpandFramework/DevExpress.XAF/stargazers

    If our packages are helping your business and you want to sustain our activities please consider becoming a sponor or a backer https://opencollective.com/expand.
"@
    Set-Content "$sourceDir\bin\Readme.txt" $message 
    $packageName=[System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $assembly=$assemblyVersions|Where-Object{$_.name -eq $packageName}
    $name=$_.FullName
    $directory=$_.Directory.Parent.FullName
    Write-Host "Packing $($assembly.Version) $name $version " -f Blue
    & (Get-XNugetPath) pack $name -OutputDirectory $($packData.nugetBin) -Basepath $directory -Version $($assembly.Version)
    if ($lastexitcode){
        throw $_.Exception
    }
}

    
