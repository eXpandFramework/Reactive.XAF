
$SourcePath = "$PSScriptRoot\.."
        
Set-Location $SourcePath
$stage = "$Sourcepath\buildstage"
New-Item $stage -ItemType Directory -Force
Get-ChildItem $stage -Recurse | Remove-Item -Recurse -Force
New-Item $stage\source -ItemType Directory -Force
Set-Location $stage
New-Item "$stage\TestApplication" -ItemType Directory
Write-Host "Copying Bin" 
Move-Item "$Sourcepath\Bin" "$stage\Bin" -Force
        
        
Write-Host "Moving TestWinApplication" 
Move-Item "$stage\Bin\TestWinApplication" "$stage\TestApplication" -Force 
Write-Host "Moving TestWebApplication" 
Move-Item "$stage\Bin\TestWebApplication" "$stage\TestApplication" -Force 
Write-Host "Moving TestWinDesktopApplication" 
Move-Item "$stage\Bin\TestWinDesktopApplication" "$stage\TestApplication" -Force 
Write-Host "Moving AllTestsWeb" 
Move-Item "$stage\Bin\AllTestWeb" "$stage\TestApplication" -Force 
Write-Host "Moving AllTestsWin" 
Move-Item "$stage\Bin\AllTestWin" "$stage\TestApplication" -Force 
Remove-Item "$stage\bin\ReactiveLoggerClient" -Recurse -Force
    
"Web", "Win" | ForEach-Object {
    Write-HostFormatted "Zipping DX $_" -ForegroundColor Magenta
    $dxassemblies = ((Get-ChildItem "$stage\TestApplication\AllTest$_" DevExpress*.dll -Recurse) + (Get-ChildItem ("$stage\TestApplication\Test$_", "Application" -join "") DevExpress*.dll -Recurse))
    New-Item $stage\DX$_ -ItemType Directory -Force
    $dxassemblies | Move-Item -Destination $stage\DX$_ -Force
    Compress-Files $stage\DX$_ $stage\DX$_.Zip -compressionLevel NoCompression 
    Remove-Item $stage\DX$_ -Force -Recurse
    Get-ChildItem "$stage\bin" DevExpress*.dll | Remove-Item
    New-Item $stage\DX -ItemType Directory -Force
    Move-Item $stage\DX$_.Zip $stage\DX
}

Write-HostFormatted "Zipping DX Win netcoreapp3.1" -ForegroundColor Magenta
$dxassemblies = ((Get-ChildItem "$stage\TestApplication\AllTestWin\netcoreapp3.1" DevExpress*.dll -Recurse) + (Get-ChildItem "$stage\TestApplication\TestWinDesktopApplication" DevExpress*.dll -Recurse))
New-Item $stage\DXWinnetcoreapp3.1 -ItemType Directory -Force
$dxassemblies | Move-Item -Destination $stage\DXWinnetcoreapp3.1 -Force
Compress-Files $stage\DXWinnetcoreapp3.1 $stage\DXWinnetcoreapp3.1.Zip -compressionLevel NoCompression 
Remove-Item $stage\DXWinnetcoreapp3.1 -Force -Recurse
Move-Item $stage\DXWinnetcoreapp3.1.Zip $stage\DX
        
Write-HostFormatted "FINISH" -Section