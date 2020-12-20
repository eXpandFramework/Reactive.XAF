param(
    $sourceDir
)
$assemblyInfo = Get-Content "$sourceDir\src\Common\AssemblyInfoVersion.cs"
$version=[System.Text.RegularExpressions.Regex]::Match($assemblyInfo, 'Version = "([^"]*)').Groups[1].Value
$modules=(Get-ChildItem "$sourceDir\src\Modules" "*.csproj" -Recurse)
$extensions=(Get-ChildItem "$sourceDir\src\Extensions" "*.csproj" -Recurse)
$testslibs=(Get-ChildItem "$sourceDir\src\Tests" "*Xpand.TestsLib*.csproj" -Recurse)
$modules+$extensions+$testslibs | ForEach-Object {
    [PSCustomObject]@{
        Name    = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
        Version = $version
    }
}