param($version="18.2.300.0")
$ErrorActionPreference = "Stop"
$root=[System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\")

Set-Location $root
New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force |Out-Null
& $root\tools\NuGet.exe spec -Force -verbosity quiet 
$template = "$root\Package.nuspec"

get-childitem "$root\src\" -Include "*.csproj" -Exclude "*.Specifications.*","*.Source.*" -Recurse | ForEach-Object {
    [xml]$nuspec = Get-Content $template
    $metaData = $nuspec.Package.Metadata
    $metaData.dependencies.dependency.parentnode.removechild($metaData.dependencies.dependency)|Out-Null
    $projectPath=$_.FullName
    [xml]$csproj = get-content $projectPath
    $metaData.Id = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
    $metaData.description=$metaData.id
    write-host "Creating Nuspec for $($metaData.Id)" -f "Blue"
    $relativeLocation=$_.DirectoryName.Replace($root,"").Replace("\","/")
    $metaData.projectUrl = "https://github.com/eXpandFramework/XAF/blob/master/src$relativeLocation"
    $metaData.licenseUrl = "https://github.com/eXpandFramework/XAF/blob/master/LICENSE"
    $metaData.iconUrl = "http://expandframework.com/images/site/eXpand-Sign.png"
    $metaData.authors="eXpandFramework"
    $metaData.owners="eXpandFramework"
    $metaData.releaseNotes  ="https://github.com/eXpandFramework/XAF/releases"
    $metaData.copyright="eXpandFramework.com"
    $nameTag=$metaData.id.Replace("Xpand.XAF.Modules.","").Replace("Xpand.XAF.Extensions.","")
    $metaData.tags="DevExpress XAF modules, eXpandFramework, XAF, eXpressApp,  $nameTag"
    $AddDependency={
        param($psObj)
        $dependency = $nuspec.CreateElement("dependency")
        $dependency.SetAttribute("id", $psObj.id)
        $dependency.SetAttribute("version", $psObj.version)
        $psObj.id
        $nuspec.SelectSingleNode("//dependencies").AppendChild($dependency)|Out-Null
    }
    $versionConverter=[PSCustomObject]@{
        id      = "Xpand.VersionConverter"
        version = "1.0.0"
        targetFramework="net452"
    }
    Invoke-Command $AddDependency -ArgumentList $versionConverter
    $targetFrameworkVersion="$($csproj.Project.PropertyGroup.TargetFrameworkVersion)".Substring(1).Replace(".","").Trim()      
    $csproj.Project.ItemGroup.Reference.Include|Where-Object {"$_".StartsWith("Xpand.XAF")}|ForEach-Object {
        if (!$version){
            $packageName=$_
            Get-ChildItem $root *.csproj -Recurse|Where-Object{
                $f=[System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
                if ($f -eq $packageName){
                    $assemblyInfo=get-content "$($_.DirectoryName)\Properties\AssemblyInfo.cs"
                    $version=[System.Text.RegularExpressions.Regex]::Match($assemblyInfo,'Version\("([^"]*)').Groups[1].Value
                }
            }
        }
        $packageInfo=[PSCustomObject]@{
            id      = $_
            version = $version
            targetFramework=$targetFrameworkVersion
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

    
    "dll", "pdb"|ForEach-Object {
        $file = $nuspec.CreateElement("file")
        $file.SetAttribute("src", "$($metaData.Id).$_")
        $file.SetAttribute("target", "lib\net$targetFrameworkVersion\$($metaData.Id).$_")
        $nuspec.SelectSingleNode("//files").AppendChild($file)|Out-Null
    }
    
    New-Item -ItemType Directory -Path "$root\bin\nuspec" -Force -ErrorAction SilentlyContinue|Out-Null
    $nuspec.Save("$root\bin\nuspec\$($metadata.id).nuspec")

} 
Remove-Item $template 
