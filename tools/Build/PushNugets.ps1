param(
    $sourcesRoot="$PSScriptRoot\..\..",
    $remotePackageSource="https://xpandnugetserver.azurewebsites.net/nuget",
    $criteria=@("Xpand.XAF*","Xpand.VersionConverter*"),
    $apiKey=$null,
    $localPackageSource="$PSScriptRoot\..\..\bin\Nupkg"
)
set-location $sourcesRoot

if ($remotePackageSource -like "*nuget.org*"){
    $nugetResult=$criteria|ForEach-Object{
        & nuget  list -source $remotePackageSource id:$_
    }
}
else{
    $nugetResult=$criteria|ForEach-Object{
        $item=$_
        & nuget list -source $remotePackageSource|Where-Object{$_ -like $item}
    }
}
$packages=$nugetResult|ForEach-Object{
    $strings=$_.Split(' ')
    [PSCustomObject]@{
        Name = $strings[0]
        Version = $strings[1]
    }
}

Get-ChildItem $localPackageSource *.nupkg -Recurse|ForEach-Object{
    $localPackageName=[System.IO.Path]::GetFileNameWithoutExtension($_)
    $r=New-Object System.Text.RegularExpressions.Regex("[\d]{1,2}\.[\d]{1}\.[\d]*(\.[\d]*)?")
    $localPackageVersion=$r.Match($localPackageName).Value
    $localPackageName=$localPackageName.Replace($localPackageVersion,"").Trim(".")
    $package=$packages|Where-Object{$_.name -eq $localPackageName  }
    if (!$package -or $package.Version -ne $localPackageVersion){
        "Pushing $($_.FullName)"
        & nuget push $_.FullName -source $remotePackageSource -ApiKey $apikey
    }
}