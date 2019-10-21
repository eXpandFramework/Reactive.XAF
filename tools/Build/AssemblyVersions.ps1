param(
    $sourceDir
)
(Get-ChildItem "$sourceDir\src\Modules" "*.csproj" -Recurse)+(Get-ChildItem "$sourceDir\src\Extensions" "*.csproj" -Recurse) | ForEach-Object {
    $assemblyInfo = Get-Content "$($_.DirectoryName)\Properties\AssemblyInfo.cs"
    [PSCustomObject]@{
        Name    = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
        Version = [System.Text.RegularExpressions.Regex]::Match($assemblyInfo, 'Version\("([^"]*)').Groups[1].Value
    }
}