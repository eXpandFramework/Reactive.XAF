set-location $PSScriptRoot

Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force
Set-Location "$PSScriptRoot\src\Extensions"
dotnet build  /p:RestoreIgnoreFailedSources=true
Pop-Location
Set-Location "$PSScriptRoot\src\Modules"
dotnet build  