param(
    [string[]]$Toolpackages = "Xpand.XAF.ModelEditor",
    $Branch = "master",
    $sourceDir = "$PSScriptRoot\.."
)
$releasepackages=Get-XpandPackages Release XAFAll 
$labPackages=Get-XpandPackages Lab XAFAll
$latestPackages=Get-XpandPackages Release XAFAll|ForEach-Object{
    $package=$_
    $labPackage=$labPackages|Where-Object{$_.Id -eq $package.id}
    if ($labPackage.version -gt $package.version){
        $labPackage
    }
    else{
        $package
    }
}
$toolNuspecs=Get-ChildItem "$sourceDir\Tools\$_" *.nuspec -Recurse | Where-Object { $_.FullName -notlike "*\obj\*" }
if ($Branch -eq "master"){
    $latestPackages|Where-Object{$_.id -in $Toolpackages}|ForEach-Object{
        if ($_.Version.Revision -gt 0){
            $packageId=$_.Id
            $toolNuspec=$toolNuspecs|Where-Object{$_.BaseName -eq $packageId}
            [xml]$xml=Get-XmlContent $toolNuspec.FullName
            $xml.package.metadata.version=(Update-Version $_.version -Build)
            $xml.Save($toolNuspec.FullName)
        }
    }
}
$Toolpackages | ForEach-Object {
    $toolName=$_
    [PSCustomObject]@{
        Name   = $_
        Nuspec = ($toolNuspecs| Where-Object { $_.BaseName -eq $toolName }).FullName
    }
} | ForEach-Object {
    $packagename = $_.Name
    $publishedVersion = ($releasepackages| Where-Object { $_.id -eq $packagename }).Version
    if ($Branch -eq "lab") {
        $labVersion = ($labPackages | Where-Object { $_.id -eq $packagename }).Version
        if ($labVersion -gt $publishedVersion) {
            $publishedVersion = $labVersion
        }
    }
    
    [xml]$nuspec = Get-Content $_.Nuspec
    $nuspecVersion = ([version]$nuspec.package.metadata.Version)
    if ($publishedVersion -lt $nuspecVersion) {
        $minor = ([version]([System.Diagnostics.FileVersionInfo]::GetVersionInfo("$sourceDir\bin\Xpand.XAF.Modules.Reactive.dll").FileVersion)).Minor
        if ($nuspecVersion.Minor -ne $minor) {
            $nuspec.package.metadata.version = "$($nuspecVersion.Major).$Minor.0.0"
            $nuspec.Save($_.Nuspec)
        }
        invoke-script {& (Get-NugetPath) pack $_.Nuspec -NoPackageAnalysis -OutputDirectory $sourceDir\bin\nupkg -Basepath (Get-Item $_.Nuspec).DirectoryName}
    }    
}

