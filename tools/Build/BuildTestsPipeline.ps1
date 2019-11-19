param(
    $SourcePath="$PSScriptRoot\..\..",
    $DXApiFeed=$env:DxFeed
)
Copy-Item "$SourcePath\paket.lock" "$SourcePath\paket.lock1"

$dxAssembly = Get-ChildItem $SourcePath\bin DevExp*.dll | Select-Object -First 1
[version]$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly).FileVersion
$dxVersion="$($version.Major).$($version.Minor).$($version.Build)"
"dxVersion=$dxVersion"
& "$SourcePath\go.ps1" -InstallModules
$SourcePath, "$SourcePath\src\tests\all" | ForEach-Object {
    Set-Location $_
    Move-PaketSource 0 $DXApiFeed
}
Set-Location $SourcePath
Start-XpandProjectConverter -version $dxVersion -path $SourcePath -SkipInstall

Set-Location $SourcePath
"PaketInstall $SourcePath (due to different Version)"
Invoke-PaketInstall -Strict 
Get-Content $SourcePath\src\tests\all\paket.dependencies -Raw
Get-Content $SourcePath\paket.dependencies -Raw
& $SourcePath\go.ps1 -tasklist BuildTests -dxversion $dxVersion

Set-Location $SourcePath
$stage = "$Sourcepath\buildstage"
New-Item $stage -ItemType Directory -Force
Get-ChildItem $stage -Recurse | Remove-Item -Recurse -Force


Set-Location $stage

New-Item "$stage\Tests" -ItemType Directory
Copy-Item "$Sourcepath\Bin" "$stage\Bin" -Recurse -Force
Move-Item "$stage\Bin\TestWinApplication" "$stage\Tests" -Force
Move-Item "$stage\Bin\TestWebApplication" "$stage\Tests" -Force
Move-Item "$stage\Bin\AllTestWeb" "$stage\Tests" -Force
Move-Item "$stage\Bin\AllTestWin" "$stage\Tests" -Force
Remove-Item "$stage\bin\ReactiveLoggerClient" -Recurse -Force -ErrorAction SilentlyContinue
        
Copy-Item "$SourcePath\paket.lock1" "$SourcePath\paket.lock" -Force