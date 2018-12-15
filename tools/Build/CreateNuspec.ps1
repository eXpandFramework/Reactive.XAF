param($version="18.2.300.0")
$ErrorActionPreference = "Stop"
$root=[System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\")

Set-Location $root
New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force |Out-Null
& $root\tools\NuGet.exe spec -Force -verbosity quiet 
$template = "$root\Package.nuspec"

get-childitem "$root\src\" -Include "*.csproj" -Exclude "DevExpress.XAF.Agnostic.Specifications.*" -Recurse | ForEach-Object {
    [xml]$nuspec = Get-Content $template
    $metaData = $nuspec.Package.Metadata
    $metaData.dependencies.dependency.parentnode.removechild($metaData.dependencies.dependency)|Out-Null
    $projectPath=$_.FullName
    [xml]$csproj = get-content $projectPath
    $metaData.Id = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    $metaData.description=$metaData.id
    write-host "Creating Nuspec for $($metaData.Id)" -f "Blue"
    $relativeLocation=$_.DirectoryName.Replace($root,"").Replace("\","/")
    $metaData.projectUrl = "https://github.com/eXpandFramework/Packages/blob/master/src$relativeLocation"
    $metaData.licenseUrl = "https://github.com/eXpandFramework/Packages/blob/master/LICENSE"
    $metaData.iconUrl = "http://expandframework.com/images/site/eXpand-Sign.png"
    $metaData.authors="eXpandFramework"
    $metaData.owners="eXpandFramework"
    $metaData.releaseNotes  ="https://github.com/eXpandFramework/Packages/releases"
    $metaData.copyright="eXpandFramework.com"
    $nameTag=$metaData.id.Replace("DevExpress.XAF.Modules.","").Replace("DevExpress.XAF.Extensions.","")
    $metaData.tags="DevExpress XAF, eXpandFramework, eXpressApp, Packages, $nameTag"
    $AddDependency={
        param($psObj)
        $dependency = $nuspec.CreateElement("dependency")
        $dependency.SetAttribute("id", $psObj.id)
        $dependency.SetAttribute("version", $psObj.version)
        $psObj.id
        $nuspec.SelectSingleNode("//dependencies").AppendChild($dependency)|Out-Null
    }

    $csproj.Project.ItemGroup.Reference.Include|Where-Object {"$_".StartsWith("DevExpress.XAF")}|ForEach-Object {
        $packageInfo=[PSCustomObject]@{
            id      = $_
            version = $version
            targetFramework="net461"
        }       
        $comma = $_.IndexOf(",")
        if ($comma -ne -1 ) {
            $packageInfo.id = $_.Substring(0, $comma)
        }
        Invoke-Command $AddDependency -ArgumentList $packageInfo
    }
    
    $csproj.Project.ItemGroup.None.Include |Where-Object{$_ -eq "packages.config"}|ForEach-Object{
        $dir=[System.IO.Path]::GetDirectoryName($projectPath)
        [xml]$xml=Get-Content "$($dir.ToString())\packages.config"
        $xml.packages.package|ForEach-Object{
            $packageInfo=[PSCustomObject]@{
                id = $_.id
                version=$_.version
                targetFramework=$_.targetFramework
            }
            Invoke-Command $AddDependency -ArgumentList $packageInfo 
        }
    }
    
    $files = $nuspec.CreateElement("files")
    $nuspec.package.AppendChild($files)|Out-Null

    "dll", "pdb"|foreach {
        $file = $nuspec.CreateElement("file")
        $file.SetAttribute("src", "$($metaData.Id).$_")
        $file.SetAttribute("target", "lib\net461\$($metaData.Id).$_")
        $nuspec.SelectSingleNode("//files").AppendChild($file)|Out-Null
    }
    
    New-Item -ItemType Directory -Path "$root\bin\nuspec" -Force -ErrorAction SilentlyContinue|Out-Null
    $nuspec.Save("$root\bin\nuspec\$($metadata.id).nuspec")

} 
Remove-Item $template 
