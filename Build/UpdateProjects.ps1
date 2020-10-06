param(
    [version]$DXVersion = "20.1.7"
)
$ErrorActionPreference = "Stop"
$rootLocation = "$PSScriptRoot\..\"
Set-Location "$rootLocation\src"


$directive = "XAF$($DXVersion.Major)$($DXVersion.Minor)"
Get-ChildItem -Filter *.csproj -Recurse | ForEach-Object {
    $fileName = $_.FullName
    [xml]$projXml = Get-Content $fileName
    $projXml.Project.PropertyGroup | ForEach-Object {
        if ($_.DefineConstants) {
            if ($_.DefineConstants -match "XAF") {
                $regex = [regex] '(?is)XAF([\d]*)'
                $result = $regex.Replace($_.DefineConstants, $directive)
                $_.DefineConstants = $result
            }
            else {
                $_.DefineConstants += ";XAF$directive"
            }
        }
    }
    Update-ProjectSign $projXml $fileName "$rootLocation\src\Xpand.key\xpand.snk"
    Update-ProjectLanguageVersion $projXml
    Update-ProjectProperty $projXml DebugSymbols true
    Update-ProjectProperty $projXml DebugType full
    Remove-ProjectLicenseFile $projXml
    Update-ProjectAutoGenerateBindingRedirects $projXml $true
    if ($fileName -notlike "*.Tests.csproj" -or $fileName -like "*All*.csproj" ) {
        if ($fileName -notlike "*TestApplication.Web*.csproj") {
            Update-OutputPath $fileName "$rootLocation\bin\"
        }
        
    }
    
    if ($fileName -notlike "*all*.csproj*"){
        $target = Get-ProjectTargetFramework $projXml -FullName
        Update-ProjectProperty $projXml AppendTargetFrameworkToOutputPath ($target -ne "netstandard2.0")
    }
    
    $projXml.Save($fileName)
} 
