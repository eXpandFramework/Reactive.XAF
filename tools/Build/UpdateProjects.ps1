$ErrorActionPreference = "Stop"
$rootLocation="$PSScriptRoot\..\..\"
Set-Location "$rootLocation\src"

"Xpand.XAF.*" | ForEach-Object{
    Update-HintPath "$rootLocation" "$rootLocation\bin\" $_
}
Get-ChildItem -Filter *.csproj -Recurse |  ForEach-Object {
    $fileName = $_.FullName
    [xml]$projXml = Get-Content $fileName
    Update-ProjectSign $projXml $fileName "$rootLocation\src\Xpand.key\xpand.snk"
    Update-ProjectLanguageVersion $projXml
    Update-ProjectProperty $projXml DebugSymbols true
    Update-ProjectProperty $projXml DebugType full
    Remove-ProjectLicenseFile $projXml
    Update-ProjectAutoGenerateBindingRedirects $projXml
    if ($fileName -notlike "*.Tests.csproj" -or $fileName -like "*All*.csproj" ){
        Update-OutputPath $projXml $fileName "$rootLocation\bin\"
    }
    $projXml.Save($fileName)
} 
