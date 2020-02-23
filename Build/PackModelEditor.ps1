param(
    $sourceDir = "$PSScriptRoot\.."
)

$publishedVersion=(Get-XpandPackages Release XAFAll|Where-Object{$_.id -eq "Xpand.XAF.ModelEditor"}).Version
$labVersion=(Get-XpandPackages Lab XAFAll|Where-Object{$_.id -eq "Xpand.XAF.ModelEditor"}).Version
if ($labVersion -gt $publishedVersion){
    $publishedVersion=$labVersion
}
$nuspecPath="$sourceDir\tools\xpand.XAf.ModelEditor\Build\Xpand.XAF.ModelEditor.nuspec" 
[xml]$nuspec=Get-Content $nuspecPath
$nuspecVersion=([version]$nuspec.package.metadata.Version)
if ($publishedVersion -ne $nuspecVersion){
    $minor=([version]([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$sourceDir\bin\Xpand.XAF.Modules.Reactive.dll").FileVersion)).Minor
    if ($nuspecVersion.Minor -ne $minor){
        $nuspec.package.metadata.version="$($nuspecVersion.Major).$Minor.0.0"
        $nuspec.Save($nuspecPath)
    }
    & (Get-NugetPath) pack $nuspecPath -NoPackageAnalysis -OutputDirectory $sourceDir\bin\nupkg -Basepath "$sourceDir\tools\xpand.XAf.ModelEditor\Build"
}
