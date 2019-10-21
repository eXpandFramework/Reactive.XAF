param(
    $Branch = "master",
    $nugetBin = "$PSScriptRoot\..\..\bin\Nupkg",
    $sourceDir = "$PSScriptRoot\..\.."
)
Import-Module XpandPwsh -Force -Prefix X
$ErrorActionPreference = "Stop"
New-Item $nugetBin -ItemType Directory -Force | Out-Null
$versionConverterSpecPath = "$sourceDir\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec"
if ($Branch -match "lab") {
    [xml]$versionConverterSpec = Get-XmlContent $versionConverterSpecPath
    $v = New-Object System.Version($versionConverterSpec.Package.metadata.version)
    if ($v.Revision -eq -1) {
        $versionConverterSpec.Package.metadata.version = "$($versionConverterSpec.Package.metadata.version).0"
    }
    $versionConverterSpec.Save($versionConverterSpecPath)
    
}
& (Get-XNugetPath) pack $versionConverterSpecPath -OutputDirectory $nugetBin -NoPackageAnalysis
if ($lastexitcode) {
    throw 
}
$packData = [pscustomobject] @{
    nugetBin = $nugetBin
}

Set-Location $sourceDir
$assemblyVersions = & "$sourceDir\tools\build\AssemblyVersions.ps1" $sourceDir

Get-ChildItem "$sourceDir\tools\nuspec" "Xpand*.nuspec" -Recurse | ForEach-Object {
    if ($_ -like "Xpand.XAF*") {
        $name = $_.BaseName.Replace("Xpand.XAF.Modules.", "")
        $id = "Xpand.XAF.Modules.$name.$name" + "Module"
        $message = @"
    
    ➤ ​̲𝗣​̲𝗟​̲𝗘​̲𝗔​̲𝗦​̲𝗘​̲ ​̲𝗦​̲𝗨​̲𝗦​̲𝗧​̲𝗔​̲𝗜​̲𝗡​̲ ​̲𝗢​̲𝗨​̲𝗥​̲ ​̲𝗔​̲𝗖​̲𝗧​̲𝗜​̲𝗩​̲𝗜​̲𝗧​̲𝗜​̲𝗘​̲𝗦

        ☞  Iғ ᴏᴜʀ ᴘᴀᴄᴋᴀɢᴇs ᴀʀᴇ ʜᴇʟᴘɪɴɢ ʏᴏᴜʀ ʙᴜsɪɴᴇss ᴀɴᴅ ʏᴏᴜ ᴡᴀɴᴛ ᴛᴏ ɢɪᴠᴇ ʙᴀᴄᴋ ᴄᴏɴsɪᴅᴇʀ ʙᴇᴄᴏᴍɪɴɢ ᴀ SPONSOR ᴏʀ ᴀ BACKER.
            https://opencollective.com/expand

        ☞  ɪғ ʏᴏᴜ ʟɪᴋᴇ ᴏᴜʀ ᴡᴏʀᴋ ᴘʟᴇᴀsᴇ ᴄᴏɴsɪᴅᴇʀ ᴛᴏ ɢɪᴠᴇ ᴜs ᴀ STAR.
            https://github.com/eXpandFramework/DevExpress.XAF/stargazers 

    ➤ ​​̲𝗣​̲𝗮​̲𝗰​̲𝗸​̲𝗮​̲𝗴​̲𝗲​̲ ​̲𝗻​̲𝗼​̲𝘁​̲𝗲​̲𝘀

        ☞ Build the project before opening the model editor.

        ☞ To read $id documentation visit the wiki page @ https://github.com/eXpandFramework/DevExpress.XAF/wiki/$name".
        
        ☞ The package only adds the required references. To install $id add the next line in the constructor of your XAF module.
            RequiredModuleTypes.Add(typeof($id));
"@
    }
    Set-Content "$sourceDir\bin\Readme.txt" $message 
    $packageName = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
    $assemblyItem = $assemblyVersions | Where-Object { $_.name -eq $packageName }
    $name = $_.FullName
    $version=$assemblyItem.Version
    if ($packageName -like "*All"){
        [xml]$coreNuspec=Get-Content "$sourceDir\tools\nuspec\$packagename.nuspec"
        $version=$coreNuspec.package.metadata.Version
    }
    Write-Host "Packing $($assemblyItem.Version) $name $version " -f Blue
    & (Get-XNugetPath) pack $name -OutputDirectory $($packData.nugetBin) -Basepath "$sourceDir\bin" -Version $version
    if ($lastexitcode) {
        throw $_.Exception
    }
}

    
