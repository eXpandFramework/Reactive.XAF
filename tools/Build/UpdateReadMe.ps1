$ErrorActionPreference = "Stop"
$rootLocation = "$PSScriptRoot\..\.."
Set-Location $rootLocation

$nuget = Get-NugetPath
$packagesPath = "$rootLocation\bin\Nupkg\"
$packages = & $nuget List -Source $packagesPath|ConvertTo-PackageObject |Select-Object -ExpandProperty Name
Get-ChildItem "$rootLocation\src" *.csproj -Recurse|Select-Object|ForEach-Object {
    $readMePath="$($_.DirectoryName)\Readme.md"
    if ((Test-path $readMePath) -and $packages.Contains($_.BaseName)) {
        $metadata=((Get-NugetPackageSearchMetadata -Name $_.BaseName -Source $packagesPath).DependencySets.Packages|ForEach-Object{
            "$($_.Id)|$($_.VersionRange.MinVersion)`r`n"
        })
        [xml]$csproj=Get-Content $_.FullName
        $dxDepends=$csproj.Project.ItemGroup.Reference|Where-Object{$_.Include -like "DevExpress*"}|ForEach-Object{
            "**$($_.Include -creplace '(.*)\.v[\d]{2}\.\d', '$1')**|**Any**"
        }
        $metadata="Name|Version`r`n----|----`r`n$dxDepends`r`n$metadata"
        $readMe=Get-Content $readMePath -Raw
        if ($readMe -notmatch "## Dependencies"){
            $readMe=$readMe.Replace("## Issues","## Dependencies`r`n## Issues")
        }
        
        $version=$csproj.Project.PropertyGroup.TargetFrameworkVersion|Select-Object -First 1
        $result = $readMe -creplace '## Dependencies([^#]*)', @"
## Dependencies
``.NetFramework: $version```r`n
$metadata`r`n
"@
        Set-Content $readMePath $result.Trim()
    }   
}
