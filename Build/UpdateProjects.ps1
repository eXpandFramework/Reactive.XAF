param(
    $DXVersion = "21.2.2-beta"
)
$ErrorActionPreference = "Stop"
$rootLocation = "$PSScriptRoot\..\"
Set-Location "$rootLocation\src"


$directive = "XAF$(Get-VersionPart $DXVersion Minor)".Replace(".","")
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
    Update-ProjectProperty $projXml ProduceReferenceAssembly false
    Update-ProjectProperty $projXml DebugType full
    Update-ProjectProperty $projXml CopyLocalLockFileAssemblies true
    Remove-ProjectLicenseFile $projXml
    Update-ProjectAutoGenerateBindingRedirects $projXml $true
    if ($fileName -notlike "*.Tests.csproj" -or $fileName -like "*EasyTest*.csproj" ) {
        if ($fileName -notlike "*TestApplication.Web*.csproj") {
            Update-OutputPath $fileName "$rootLocation\bin\"|Out-Null
        }
        
    }
    if ( $fileName -match "DocumentStyleManager" ){
        if ($DXVersion -lt "20.1.7"){
            Update-ProjectProperty $projXml TargetFramework "net461"
            $styleTestProj=Get-XmlContent "$rootLocation\src\tests\Office.DocumentStyleManager\Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.csproj"
            $styleTestProj.Project.ItemGroup.Reference|Where-Object{$_.Include -eq "Xpand.XAF.Modules.Office.DocumentStyleManager"}|ForEach-Object{
                $_.HintPath="..\..\..\bin\net461\Xpand.XAF.Modules.Office.DocumentStyleManager.dll"
            }
            $styleTestProj.save("$rootLocation\src\tests\Office.DocumentStyleManager\Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.csproj")
        }
    }
    if ($fileName -notlike "*EasyTest*.csproj*"){
        # $target = Get-ProjectTargetFramework $projXml -FullName
        # Update-ProjectProperty $projXml AppendTargetFrameworkToOutputPath ($target -notlike "netstandard2.*")
    }
    
    $target = Get-ProjectTargetFramework $projXml -FullName
    $regex = [regex] '(?<t>.*-windows$)'
    $result = $regex.Replace($target, '${t}7.0')
    $projXml.Project.PropertyGroup|Where-Object{$_["TargetFramework"]}|ForEach-Object{
        $_.TargetFramework=$result
    }
    $projXml.Save($fileName)|Out-null
} 
