param(
    
)
# Get-ChildItem|Move-Item -Destination
$SourcePath = "C:\Work\eXpandFramework\DevExpress.XAF\\"
$dxAssembly = Get-ChildItem $SourcePath\bin DevExp*.dll | Select-Object -First 1
[version]$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly).FileVersion
Get-ChildItem $SourcePath\Source | Move-Item -Destination $SourcePath -Force -ErrorAction SilentlyContinue
dotnet tool restore
"dxVersion=$($version.Major).$($version.Minor).$($version.Build)"

& $SourcePath\go.ps1 -tasklist BuildTests -version "$($version.Major).$($version.Minor).$($version.Build)"