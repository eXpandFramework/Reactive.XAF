param(
    $Branch="master",
    $sourcesRoot="$PSScriptRoot\..\..",
    $apiKey=(Get-NugetApiKey),
    $criteria=@("Xpand.XAF*","Xpand.VersionConverter*"),
    $localPackageSource="$PSScriptRoot\..\..\bin\Nupkg"
)
& "$PSScriptRoot\CreateNuspec.ps1"

set-location $sourcesRoot
$remotePackageSource=Get-PackageFeed -Nuget
if ($Branch -eq "lab"){
    $remotePackageSource=Get-PackageFeed -Xpand
}

if ($remotePackageSource -like "*nuget.org*"){
    $nugetResult=$criteria|ForEach-Object{
        & (Get-Nugetpath)  list -source $remotePackageSource id:$_
    }
}
else{
    $nugetResult=$criteria|ForEach-Object{
        $item=$_
        & (Get-Nugetpath) list -source $remotePackageSource|Where-Object{$_ -like $item}
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
    "localPackageVersion=$localPackageVersion"
    $localPackageName=$localPackageName.Replace($localPackageVersion,"").Trim(".")
    "localPackageName=$localPackageName"
    $package=$packages|Where-Object{$_.name -eq $localPackageName  }
    "package=$package"
    if (!$package -or $package.Version -ne $localPackageVersion){
        "Pushing $($_.FullName)"
        & (Get-Nugetpath) push $_.FullName -source $remotePackageSource -ApiKey $apikey
    }
}
