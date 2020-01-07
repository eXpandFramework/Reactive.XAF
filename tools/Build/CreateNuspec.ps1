param(
    $root = [System.IO.Path]::GetFullPath("$PSScriptRoot\..\..\"),
    [switch]$Release,
    $dxVersion=$env:FirstDxVersion,
    $branch
)

$ErrorActionPreference = "Stop"
# Import-XpandPwsh

New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force | Out-Null
[version]$modulesVersion=[System.Diagnostics.FileVersionInfo]::GetVersionInfo("$root\bin\Xpand.XAF.Modules.Reactive.dll" ).FileVersion
$versionConverterPath="$root\tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec"
[xml]$nuspec=Get-Content $versionConverterPath
[version]$vv=$nuspec.package.metadata.version
$nuspec.package.metadata.version="$($vv.major).$($modulesVersion.Minor).$($vv.build)"
$nuspec.Save($versionConverterPath)

$allProjects=Get-ChildItem $root *.csproj -Recurse | Select-Object -ExpandProperty BaseName
# Get-ChildItem "$root\src\" -Include "*.csproj" -Recurse | Where-Object { $_ -notlike "*Test*" } | ForEach-Object {
Get-ChildItem "$root\src\" -Include "*.csproj" -Recurse | Where-Object { $_ -notlike "*Test*" } | Invoke-Parallel -VariablesToImport @("allProjects","root","Release") -Script {
    Set-Location $root
    $projectPath = $_.FullName
    Write-Host "Creating Nuspec for $($_.baseName)" -f "Blue"
    $uArgs = @{
        NuspecFilename           = "$root\tools\nuspec\$($_.baseName).nuspec"
        ProjectFileName          = $projectPath
        ReferenceToPackageFilter = "Xpand*"
        PublishedSource          = (Get-PackageFeed -Xpand)
        Release                  = $Release
        ReadMe                   = $false
        AllProjects             = $allProjects
    }
    if (!(Test-Path $uArgs.NuspecFilename)) {
        Set-Location $root\tools\nuspec
        & (Get-NugetPath) spec $_.BaseName
    }
    if ($Release) {
        $uArgs.PublishedSource = (Get-PackageFeed -Nuget)
    }
    
    Update-Nuspec @uArgs 

    $nuspecFileName = "$root\tools\nuspec\$($_.BaseName).nuspec"
    [xml]$nuspec = Get-Content $nuspecFileName

    $readMePath = "$($_.DirectoryName)\ReadMe.md"
    if (Test-Path $readMePath) {
        $readMe = Get-Content $readMePath -Raw
        if ($readMe -cmatch '# About([^#]*)') {
            $nuspec.package.metaData.description = "$($matches[1])".Trim()
        }
        else {
            $nuspec.package.metaData.description = $nuspec.package.metaData.id
        }
    }
    else {
        $nuspec.package.metaData.description = $nuspec.package.metaData.id
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
    
    if ($nuspec.package.metaData.id -like "Xpand.XAF*" -or $nuspec.package.metaData.id -like "Xpand.Extensions.XAF*") {
        "versionConverter"
        $versionConverter = [PSCustomObject]@{
            id              = "Xpand.VersionConverter"
            version         = ([xml](Get-Content "$root\Tools\Xpand.VersionConverter\Xpand.VersionConverter.nuspec")).package.metadata.version
            targetFramework = "net452"
        }
        $versionConverter |Out-String
        Add-NuspecDependency $versionConverter.Id $versionConverter.Version $nuspec
    }
    
    
    $nuspec.Save($nuspecFileName)
} 
& "$root\tools\build\UpdateAllNuspec.ps1" $root $Release $branch