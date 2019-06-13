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
    Update-ProjectDebugSymbols $projXml
    Remove-ProjectLicenseFile $projXml
    Update-ProjectAutoGenerateBindingRedirects $projXml
    Update-OutputPath $projXml $fileName "$rootLocation\bin\"
    $projXml.Save($fileName)
} 
