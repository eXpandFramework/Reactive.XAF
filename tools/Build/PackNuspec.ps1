param(
    $nugetBin="$PSScriptRoot\..\..\bin\Nupkg",
    $nugetExe="$PSScriptRoot\..\nuget.exe",
    $sourceDir="$PSScriptRoot\..\.."
)
$ErrorActionPreference="Stop"
New-Item $nugetBin -ItemType Directory -Force|Out-Null
& $nugetExe pack "$sourceDir\Tools\Xpand.VersionConverter.nuspec" -OutputDirectory $nugetBin -NoPackageAnalysis
if ($lastexitcode){
    throw 
}
$packData = [pscustomobject] @{
    nugetBin = $nugetBin
    nugetExe = $nugetExe
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
    $packageName=[System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $assembly=$assemblyVersions|Where-Object{$_.name -eq $packageName}
    $name=$_.FullName
    $directory=$_.Directory.Parent.FullName
    & $packData.nugetExe pack $name -OutputDirectory $($packData.nugetBin) -Basepath $directory -Version $($assembly.Version)
    if ($lastexitcode){
        throw $_.Exception
    }
}

    
