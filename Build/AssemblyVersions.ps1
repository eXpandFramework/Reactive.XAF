param(
    $sourceDir
)
$assemblyInfo = Get-Content "$sourceDir\src\Common\AssemblyInfoVersion.cs"
$version=[System.Text.RegularExpressions.Regex]::Match($assemblyInfo, 'Version = "([^"]*)').Groups[1].Value
(Get-ChildItem "$sourceDir\src\Modules" "*.csproj" -Recurse)+(Get-ChildItem "$sourceDir\src\Extensions" "*.csproj" -Recurse) | ForEach-Object {
    [PSCustomObject]@{
        Name    = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
        Version = $version
    }
}