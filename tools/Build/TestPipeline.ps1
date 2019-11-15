param(
    
)
Get-ChildItem|Move-Item -Destination
$dxAssembly=Get-ChildItem $SourcePath\bin DevExp*.dll|Select-Object -First 1
$version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly).FileVersion

& $SourcePath\go.ps1 -tasklist BuildTests -version "$($version.Major).$($version.Minor).$($version.Build)"