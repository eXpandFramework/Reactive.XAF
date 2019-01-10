param(
    [parameter()]
    $root="..\..",
    [parameter()]
    $source="https://xpandnugetserver.azurewebsites.net/nuget",
    [parameter()]
    $criteria="DevExpress.XAF*",
    [parameter()]
    $apiKey=$null
)
set-location $root
$nugetExe="$PSScriptRoot\..\Nuget.exe"
if ($source -like "*nuget.org*"){
    $nugetResult=& $nugetExe  list -source $source id:$criteria
}
else{
    $nugetResult=& $nugetExe list -source $source|Where-Object{$_ -like $criteria}
}
$packages=$nugetResult|ForEach-Object{
    $strings=$_.Split(' ')
    [PSCustomObject]@{
        Name = $strings[0]
        Version = $strings[1]
    }
}

Get-ChildItem *.nupkg -Recurse|ForEach-Object{
    $localPackageName=[System.IO.Path]::GetFileNameWithoutExtension($_)
    $r=New-Object System.Text.RegularExpressions.Regex("[\d]{2}\.[\d]{1}\.[\d]*(\.[\d]*)?")
    $localPackageVersion=$r.Match($localPackageName).Value
    $localPackageName=$localPackageName.Replace($localPackageVersion,"").Trim(".")
    $package=$packages|Where-Object{$_.name -eq $localPackageName  }
    if ($package.Version -ne $localPackageVersion){
        "Pushing $($_.FullName)"
        & $nugetExe push $_.FullName -source $source -ApiKey $apikey
    }
}