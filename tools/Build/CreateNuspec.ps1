$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\")

Set-Location $root
New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force |Out-Null
& (Get-NugetPath) spec -Force -verbosity quiet 

$template = "$root\Package.nuspec"
$versionConverter = [PSCustomObject]@{
    id              = "Xpand.VersionConverter"
    version         = ([xml](get-content "$PsScriptRoot\..\Xpand.VersionConverter\Xpand.VersionConverter.nuspec")).package.metadata.version
    targetFramework = "net452"
}

get-childitem "$root\src\" -Include "*.csproj" -Exclude "*.Tests.*", "*.Source.*" -Recurse | ForEach-Object {
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
    $metaData.projectUrl = "https://github.com/eXpandFramework/XAF/blob/master/src$relativeLocation"
    $metaData.licenseUrl = "https://github.com/eXpandFramework/XAF/blob/master/LICENSE"
    $metaData.iconUrl = "http://expandframework.com/images/site/eXpand-Sign.png"
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
    
    $targetFrameworkVersion = "$($csproj.Project.PropertyGroup.TargetFrameworkVersion)".Substring(1).Trim()      
    $targetFrameworkAttribute=$nuspec.CreateAttribute("targetFramework")
    $targetFrameworkAttribute.Value=".NETFramework$targetFrameworkVersion"
    $groupElement=$nuspec.CreateElement("group")
    $groupElement.Attributes.Append($targetFrameworkAttribute)
    $nuspec.SelectSingleNode("//dependencies").AppendChild($groupElement)
    if ($metaData.id -like "Xpand.XAF*"){
        Invoke-Command $AddDependency -ArgumentList $versionConverter
    }
    $csproj.Project.ItemGroup.Reference.Include|Where-Object {"$_".StartsWith("Xpand.XAF")}|ForEach-Object {
        $packageName = $_
        $version = Get-ChildItem $root *.csproj -Recurse|Where-Object {
            [System.IO.Path]::GetFileNameWithoutExtension($_.FullName) -eq $packageName
        }|ForEach-Object {
            $assemblyInfo = get-content "$($_.DirectoryName)\Properties\AssemblyInfo.cs"
            [System.Text.RegularExpressions.Regex]::Match($assemblyInfo, 'Version\("([^"]*)').Groups[1].Value
        }|Select-Object -First 1
        $packageInfo = [PSCustomObject]@{
            id              = $_
            version         = $version
            targetFramework = $targetFrameworkVersion.Replace(".", "")
        }       
        $comma = $_.IndexOf(",")
        if ($comma -ne -1 ) {
            $packageInfo.id = $_.Substring(0, $comma)
        }
        Invoke-Command $AddDependency -ArgumentList $packageInfo
    }
    
    
    $csproj.Project.ItemGroup.None.Include |Where-Object {$_ -eq "packages.config"}|ForEach-Object {
        $dir = [System.IO.Path]::GetDirectoryName($projectPath)
        [xml]$xml = Get-Content "$($dir.ToString())\packages.config"
        $xml.packages.package|ForEach-Object {
            $packageInfo = [PSCustomObject]@{
                id              = $_.id
                version         = $_.version
                targetFramework = $_.targetFramework
            }
            Invoke-Command $AddDependency -ArgumentList $packageInfo 
        }
    }
    
    $files = $nuspec.CreateElement("files")
    $nuspec.package.AppendChild($files)|Out-Null

    
    "dll", "pdb"|ForEach-Object {
        $file = $nuspec.CreateElement("file")
        $file.SetAttribute("src", "$($metaData.Id).$_")
        $file.SetAttribute("target", "lib\net$targetFrameworkVersion\$($metaData.Id).$_")
        $nuspec.SelectSingleNode("//files").AppendChild($file)|Out-Null
    }
    
    if ($metaData.id -like "Xpand.XAF*"){
        $file = $nuspec.CreateElement("file")
        $file.SetAttribute("src", "Readme.txt")
        $file.SetAttribute("target", "")
        $nuspec.SelectSingleNode("//files").AppendChild($file)|Out-Null
    }

    New-Item -ItemType Directory -Path "$root\bin\nuspec" -Force -ErrorAction SilentlyContinue|Out-Null
    $nuspec.Save("$root\bin\nuspec\$($metadata.id).nuspec")

} 
Remove-Item $template 
