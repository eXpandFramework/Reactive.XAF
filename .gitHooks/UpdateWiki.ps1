set-location C:\Work\eXpandFramework\DevExpress.XAF.wiki\

$root=(Get-Item "$PSScriptRoot\..").FullName
Copy-Item "$root\Readme.md" ".\Home.md" -Force

Get-ChildItem $root Readme.md -Recurse|Where-Object{$_.DirectoryName -ne $root}| ForEach-Object{
    $name=$_.Directory.Name.Replace("Xpand.","")
    Copy-item $_.FullName ".\$name.md" -Force
}

git add -A 
$changedFiles=(git diff --cached --name-only) -join "`r`n"
$msg=$changedFiles -join ", "
if ($msg){
    git commit -m $msg
    New-BurntToastNotification -Text "DevExpress.XAF.Wiki changed`r`n$changedFiles" -Sound 'Alarm2' -SnoozeAndDismiss
}


