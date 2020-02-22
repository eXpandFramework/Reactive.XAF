$SourcePath="$PSScriptRoot\..\.."
Get-Content "$SourcePath\go.ps1" | Select-String dxVersion | Select-Object -First 1 | ForEach-Object {
    $regex = [regex] '"([^"]*)'
    $regex.Match($_).Groups[1].Value;
}