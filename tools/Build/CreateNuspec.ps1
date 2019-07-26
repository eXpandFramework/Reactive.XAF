param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    [switch]$Release
)

$ErrorActionPreference = "Stop"

Set-Location $root
New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force |Out-Null

$versionConverter = [PSCustomObject]@{
    id              = "Xpand.VersionConverter"
    version         = ([xml](get-content "$PsScriptRoot\..\Xpand.VersionConverter\Xpand.VersionConverter.nuspec")).package.metadata.version
    targetFramework = "net452"
}


get-childitem "$root\src\" -Include "*.csproj" -Exclude "*Tests*", "*.Source.*" -Recurse | ForEach-Object {
    $projectPath = $_.FullName
    write-host "Creating Nuspec for $($_.baseName)" -f "Blue"
    $uArgs=@{
        NuspecFilename="$root\tools\nuspec\$($_.baseName).nuspec"
        ProjectFileName=$projectPath
        ReferenceToPackageFilter="Xpand.XAF*"
        PublishedSource=(Get-PackageFeed -Xpand)
        Release=$Release
        ReadMe=$true
        LibrariesFolder="$root\src\libs"
        ProjectsRoot=$root
    }
    if ($Release){
        $uArgs.PublishedSource=(Get-PackageFeed -Nuget)
    }
    Update-Nuspec @uArgs 

    $nuspecFileName="$root\tools\nuspec\$($_.BaseName).nuspec"
    [xml]$nuspec = Get-Content $nuspecFileName
    # $nuspec.package.metaData.Id = $_.BaseName
    $readMePath = "$($_.DirectoryName)\ReadMe.md"
    if (Test-Path $readMePath) {
        $readMe = Get-Content $readMePath -Raw
        if ($readMe -cmatch '# About([^#]*)') {
            $nuspec.package.metaData.description = "$($matches[1])".Trim()
        }
        else {
            $nuspec.package.metaData.description = $metaData.id
        }
    }
    else {
        $nuspec.package.metaData.description = $metaData.id
    }

    $relativeLocation = $_.DirectoryName.Replace($root, "").Replace("\", "/")
    $nuspec.package.metaData.projectUrl = "https://github.com/eXpandFramework/DevExpress.XAF/blob/master/$relativeLocation"
    $nuspec.package.metaData.licenseUrl = "https://github.com/eXpandFramework/DevExpress.XAF/blob/master/LICENSE"
    $nuspec.package.metaData.iconUrl = "http://sign.expandframework.com"
    $nuspec.package.metaData.authors = "eXpandFramework"
    $nuspec.package.metaData.owners = "eXpandFramework"
    $nuspec.package.metaData.releaseNotes = "https://github.com/eXpandFramework/DevExpress.XAF/releases"
    $nuspec.package.metaData.copyright = "eXpandFramework.com"
    $nameTag = $nuspec.package.metaData.id.Replace("Xpand.XAF.Modules.", "").Replace("Xpand.XAF.Extensions.", "")
    $nuspec.package.metaData.tags = "DevExpress XAF modules, eXpandFramework, XAF, eXpressApp,  $nameTag"
    
    $ns = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
    $ns.AddNamespace("ns", $nuspec.DocumentElement.NamespaceURI)
    $AddDependency = {
        param($psObj)
        $dependency = $nuspec.CreateElement("dependency", $nuspec.DocumentElement.NamespaceURI)
        $dependency.SetAttribute("id", $psObj.id)
        $dependency.SetAttribute("version", $psObj.version)
        $nuspec.SelectSingleNode("//ns:dependencies", $ns).AppendChild($dependency)|Out-Null
    }
    
    if ($nuspec.package.metaData.id -like "Xpand.XAF*"){
        Invoke-Command $AddDependency -ArgumentList $versionConverter
    }
    $nuspec.Save($nuspecFileName)
} 

Get-ChildItem "$root\tools\nuspec" *.nuspec|ForEach-Object{
    [xml]$nuspec=Get-Content $_.FullName
    $nuspec.package.metaData.dependencies.dependency|Where-Object{$_.Id -like "DevExpress*"}|ForEach-Object{
        $_.ParentNode.RemoveChild($_)
    }
    $nuspec.Save($_.FullName)
}
