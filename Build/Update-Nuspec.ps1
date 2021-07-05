param (
    [parameter(Mandatory)]
    [string]$NuspecFilename,
    [parameter(Mandatory)]
    [string]$ProjectFileName,
    [parameter(Mandatory)]
    [string[]]$allProjects,
    [string]$ReferenceToPackageFilter = "*",
    [string]$PublishedSource,
    [switch]$Release,
    [switch]$ReadMe,
    [string]$NuspecMatchPattern,   
    [string]$LibrariesFolder ,
    $customPackageLinks = @{ },
    [switch]$KeepDependencies
)

[xml]$csproj = Get-Content $ProjectFileName
[xml]$nuspec = Get-Content $NuspecFilename
if (!$KeepDependencies) {
    if ($nuspec.package.metadata.dependencies) {
        $nuspec.package.metadata.dependencies.RemoveAll()
    }
}
if (!$KeepFiles) {
    if ($nuspec.package.files) {
        $nuspec.package.files.RemoveAll()
    }
}
$nuspec.Save($NuspecFilename)
$ns = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
$ns.AddNamespace("ns", $nuspec.DocumentElement.NamespaceURI)
        
$NuspecsDirectory = (Get-Item $NuspecFilename).DirectoryName
$projectDirectory = ((Get-Item $ProjectFileName).DirectoryName)
$id = (get-item $ProjectFileName).BaseName.Trim()
Push-Location $projectDirectory
$outputPath = $csproj.Project.PropertyGroup.OutputPath | Select-Object -First 1
if (!$outputPath) {
    throw "$ProjectFileName outputpath not set"
}
$outputPath = "$(Resolve-Path $outputPath)"
$targetFrameworkVersion = Get-ProjectTargetFramework $csproj -FullName
$appendTargetFrameworkToOutputPath = $csproj.Project.PropertyGroup.AppendTargetFrameworkToOutputPath -eq "true"
if ($appendTargetFrameworkToOutputPath -and ($targetFrameworkVersion -notmatch "netstandard")) {
    $outputPath += "\$targetFrameworkVersion"
}
$extension = "dll"
if ($csproj.Project.PropertyGroup.OutputType -eq "WinExe") {
    $extension = "exe"
}
        
$assemblyPath = "$outputPath\$id.$extension"
        
$allDependencies = @()

$fileVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath).FileVersion

$nuspec.package.metadata.version = "$fileVersion"
        
$csproj.Project.ItemGroup.Reference | Where-Object { "$($_.Include)" -like $ReferenceToPackageFilter } | ForEach-Object {
    $packageName = $_.Include
    $comma = $packageName.IndexOf(",")
    if ($comma -ne -1 ) {
        $packageName = $packageName.Substring(0, $comma)
    }
    if (!$ResolveNugetDependecies -or $packageName -in $allDependencies) {
        $matchedPackageName = $customPackageLinks[$packageName]
        if (!$matchedPackageName) {
            $projectName = $allProjects | Where-Object { $_ -eq $packageName } | Select-Object -First 1
            $regex = [regex] $NuspecMatchPattern
            $projectName = $regex.Replace($projectName, '') 
            $matchedPackageName = Get-ChildItem $NuspecsDirectory *.nuspec | Where-Object { $_.BaseName -eq $projectName }
            if ($matchedPackageName) {
                [xml]$xml = Get-Content $matchedPackageName.FullName
                $matchedPackageName = $xml.package.metadata.id    
            }
        }
        if ($matchedPackageName -ne $nuspec.package.metadata.Id) {
            Push-Location $projectDirectory
            $packagePath = Resolve-Path $_.HintPath
            Pop-Location
            $version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$packagePath").FileVersion
            Add-NuspecDependency $matchedPackageName $version $nuspec ($targetFrameworkVersion.Split("-")[0])| Out-Null
        }
                
        $nuspec.Save($NuspecFilename)
    }
}
        
$packageReference = Get-PackageReference $ProjectFileName 
        
$packageReference | Where-Object { $_.Include -and $_.PrivateAssets -ne "all" } | ForEach-Object {
    if (!$ResolveNugetDependecies -or $_.Include -in $allDependencies) {
        Add-NuspecDependency $_.Include $_.version $nuspec ($targetFrameworkVersion.Split("-")[0])| Out-Null
    }
}
$nuspec.Save($NuspecFilename)
$sourcePath = $null
        
if ($targetFrameworkVersion -notmatch "netstandard" -and $appendTargetFrameworkToOutputPath) {
    $sourcePath = "$targetFrameworkVersion\"    
}
$file = $nuspec.CreateElement("file", $nuspec.DocumentElement.NamespaceURI)
$file.SetAttribute("src", "$sourcePath$($id).$extension")
$file.SetAttribute("target", "lib\$($targetFrameworkVersion.Split("-")[0])\$id.$extension")
$nuspec.SelectSingleNode("//ns:files", $ns).AppendChild($file) | Out-Null
$file = $nuspec.CreateElement("file", $nuspec.DocumentElement.NamespaceURI)
$file.SetAttribute("src", "$sourcePath$($id).pdb")
$file.SetAttribute("target", "lib\$($targetFrameworkVersion.Split("-")[0])\$id.pdb")
$nuspec.SelectSingleNode("//ns:files", $ns).AppendChild($file) | Out-Null

$nuspec.Save($NuspecFilename)
if ($LibrariesFolder) {
    [System.Environment]::CurrentDirectory = $projectDirectory
    $libs = Get-ChildItem $LibrariesFolder *.dll
    $csproj.Project.ItemGroup.Reference.HintPath | ForEach-Object {
        if ($_) {
            Push-Location $projectDirectory
            $hintPath = Resolve-Path $_
            Pop-Location
            if (Test-Path $hintPath) {
                if ($libs | Select-Object -ExpandProperty FullName | Where-Object { $_ -eq $hintPath }) {
                    $file = $nuspec.CreateElement("file")
                    $libName = (Get-item $hintpath).Name
                    $relativePath = Get-RelativePath "$outputPath\$libname" "$LibrariesFolder"
                    $file.SetAttribute("src", "$relativePath\$libname")
                        
                    $file.SetAttribute("target", "lib\net$targetFrameworkVersion\$libName")
                    $nuspec.SelectSingleNode("//ns:files", $ns).AppendChild($file) | Out-Null    
                }
            }
        }
            
    }
}
$nuspec.Save($NuspecFilename)
if ($ReadMe) {
    $file = $nuspec.CreateElement("file", $nuspec.DocumentElement.NamespaceURI)
    $file.SetAttribute("src", "Readme.txt")
    $file.SetAttribute("target", "")
    $nuspec.SelectSingleNode("//ns:files", $ns).AppendChild($file) | Out-Null
}
    
# $uniqueDependencies = $nuspec.package.metadata.dependencies.group.dependency | Where-Object { $_.id } | Sort-Object Id -Unique
# if ($packageReference) {
#     $nuspec.package.metadata.dependencies.RemoveAll()
# }
# $uniqueDependencies | ForEach-Object { Add-NuspecDependency $_.id $_.version $nuspec $targetFrameworkVersion | Out-Null }
        
# $nuspec.Save($NuspecFilename)
        

Pop-Location