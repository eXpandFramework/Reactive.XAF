param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    [switch]$Release
)
$ErrorActionPreference = "Stop"

Set-Location $root
New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force |Out-Null
& (Get-NugetPath) spec -Force -verbosity quiet 

$template = "$root\Package.nuspec"
$versionConverter = [PSCustomObject]@{
    id              = "Xpand.VersionConverter"
    version         = ([xml](get-content "$PsScriptRoot\..\Xpand.VersionConverter\Xpand.VersionConverter.nuspec")).package.metadata.version
    targetFramework = "net452"
}
if (Test-path $root\src\libs){
    
    $libs=Get-ChildItem $root\src\libs *.dll
}

get-childitem "$root\src\" -Include "*.csproj" -Exclude "*Tests*", "*.Source.*" -Recurse | ForEach-Object {
    [xml]$nuspec = Get-Content $template
    $metaData = $nuspec.Package.Metadata
    $metaData.dependencies.dependency.parentnode.removechild($metaData.dependencies.dependency)|Out-Null
    $projectPath = $_.FullName
    [xml]$csproj = get-content $projectPath
    $metaData.Id = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    $readMePath = "$($_.DirectoryName)\ReadMe.md"
    if (Test-Path $readMePath) {
        $readMe = Get-Content $readMePath -Raw
        if ($readMe -cmatch '# About([^#]*)') {
            $metaData.description = "$($matches[1])".Trim()
        }
        else {
            $metaData.description = $metaData.id
        }
    }
    else {
        $metaData.description = $metaData.id
    }
    
    write-host "Creating Nuspec for $($metaData.Id)" -f "Blue"
    $relativeLocation = $_.DirectoryName.Replace($root, "").Replace("\", "/")
    $metaData.projectUrl = "https://github.com/eXpandFramework/DevExpress.XAF/blob/master/$relativeLocation"
    $metaData.licenseUrl = "https://github.com/eXpandFramework/XAF/blob/master/LICENSE"
    $metaData.iconUrl = "http://sign.expandframework.com"
    $metaData.authors = "eXpandFramework"
    $metaData.owners = "eXpandFramework"
    $metaData.releaseNotes = "https://github.com/eXpandFramework/XAF/releases"
    $metaData.copyright = "eXpandFramework.com"
    $nameTag = $metaData.id.Replace("Xpand.XAF.Modules.", "").Replace("Xpand.XAF.Extensions.", "")
    $metaData.tags = "DevExpress XAF modules, eXpandFramework, XAF, eXpressApp,  $nameTag"
    $AddDependency = {
        param($psObj)
        $dependency = $nuspec.CreateElement("dependency")
        $dependency.SetAttribute("id", $psObj.id)
        $dependency.SetAttribute("version", $psObj.version)
        $psObj.id
        $nuspec.SelectSingleNode("//group").AppendChild($dependency)|Out-Null
    }
    
    $targetFrameworkVersion = "$($csproj.Project.PropertyGroup.TargetFramework)".Substring(3)
    $targetFrameworkAttribute=$nuspec.CreateAttribute("targetFramework")
    $targetFrameworkAttribute.Value=".NETFramework$targetFrameworkVersion"
    $groupElement=$nuspec.CreateElement("group")
    $groupElement.Attributes.Append($targetFrameworkAttribute)
    $nuspec.SelectSingleNode("//dependencies").AppendChild($groupElement)
    if ($metaData.id -like "Xpand.XAF*"){
        Invoke-Command $AddDependency -ArgumentList $versionConverter
    }

    $uArgs=@{
        NuspecFilename="$root\bin\nuspec\$($metadata.id).nuspec"
        ProjectFileName=$projectPath
        ReferenceToPackageFilter="Xpand.XAF*"
        PublishedSource=(Get-PackageFeed -Xpand)
        Release=$Release
    }
    $nuspec=Update-Nuspec @uArgs
    New-Item -ItemType Directory -Path "$root\bin\nuspec" -Force -ErrorAction SilentlyContinue|Out-Null
    $nuspec.Save($NuspecFilename)

} 
Remove-Item $template 
