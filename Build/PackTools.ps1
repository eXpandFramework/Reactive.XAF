param(
    [string[]]$Toolpackages,
    $sourceDir = "$PSScriptRoot\.."
)
$Toolpackages|ForEach-Object{
    [PSCustomObject]@{
        Name = $_
        Nuspec=(Get-ChildItem "$sourceDir\Tools\$_" *.nuspec -Recurse|Where-Object{$_.FullName -notlike "*\obj\*"}).FullName
    }
}|ForEach-Object{
    $packagename=$_.Name
    $publishedVersion=(Get-XpandPackages Release XAFAll|Where-Object{$_.id -eq $packagename}).Version
    $labVersion=(Get-XpandPackages Lab XAFAll|Where-Object{$_.id -eq $packagename}).Version
    if ($labVersion -gt $publishedVersion){
        $publishedVersion=$labVersion
    }
    
    [xml]$nuspec=Get-Content $_.Nuspec
    $nuspecVersion=([version]$nuspec.package.metadata.Version)
    if ($publishedVersion -lt $nuspecVersion){
        $minor=([version]([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$sourceDir\bin\Xpand.XAF.Modules.Reactive.dll").FileVersion)).Minor
        if ($nuspecVersion.Minor -ne $minor){
            $nuspec.package.metadata.version="$($nuspecVersion.Major).$Minor.0.0"
            $nuspec.Save($_.Nuspec)
        }
        & (Get-NugetPath) pack $_.Nuspec -NoPackageAnalysis -OutputDirectory $sourceDir\bin\nupkg -Basepath (Get-Item $_.Nuspec).DirectoryName
    }    
}

