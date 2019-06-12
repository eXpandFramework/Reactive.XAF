
Get-ChildItem "..\..\src\Modules" "AssemblyInfo.cs" -Recurse|ForEach-Object{
    [version]$version=Get-AssemblyInfoVersion $_.FullName
    if ($version.Revision -gt 0){
        Update-AssemblyInfoVersion "$($version.Major).$($version.Minor).$($version.Build).0"
    }
}