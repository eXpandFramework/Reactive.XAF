# this build can be valueable if you want to modify the source 
# If only debugging, the symbols are distributed with the nuget packages. So, just download the source and add the module you want into your solution and you will be able to set breakpoints etc.
# if you modify the source of any Reactive.Xaf package and build it, there are targets that will copy the assemblies into the nuget folder replacing the ones you got from nuget.org
# You will need to clean your solution after each modification.
#USAGE:
# $p=@{
#     DXApiFeed="C:\dx\Latest"
#     SourcePath="c:\work\rx.xaf"
#     GitHubUserName="apobekiaris"
#     GitHubToken="$env:GithubToken" # this is an enviromental variable or you can add it in your profile.ps1 e.g. $env:GitHubToken="449e14b381ba3d...."
#     Force=$true
# }
# C:\Work\Reactive.XAF\Build\_QuickBuild.ps1 @p
#DEBUGGING
#Use VsCode with the PowerShell extension and you will be able to set breakpoints, stepin etc
param(
    $Branch = "master",
    $SourcePath ,
    $GitHubUserName=$env:myGitHubUserName,
    $GitHubToken = $env:myGitHubToken,
    $DXApiFeed = $env:myDxFeed,
    [switch]$SkipRepoClone,
    [switch]$Force
)
if ($PSVersionTable.Psedition -ne "Core"){
    Write-Warning "This script is desinged for Powershell core."
}
# check if latest XpandPwsh is used
if ((Get-Module Xpandpwsh -ListAvailable).Version -ne (Find-Module Xpandpwsh).version){
    throw "XpandPwsh Remote version is different. Use Update-Module XpandPwsh to update"
}
# check if DxFeed is not null. You can use a path as well. If your Dx sources path contains space consider creating a symbolic link
# e.g. new-item -ItemType SymbolicLink -Value "C:\Program Files\DevExpress 22.1\Components\System\Components\packages\" -Name Lastest -Path C:\DX\
if (!$DXApiFeed){
    throw "DXFeed is null. Consider adding a default value for this variable to your profile.ps1"
}
# check git clone parameters, feel free to use $skipRepoClone if you clone manually or even if you download the source with no cloning
if (!$SourcePath ){
    throw "SourcePath is null, set it to the path where you want to clone the Reactive.Xaf repo"
}
if (!(Test-path $SourcePath)){
    New-Item $SourcePath -ItemType Directory -ErrorAction SilentlyContinue
}
Push-Location $SourcePath
if (!$SkipRepoClone){   
    $sourceItems=Get-ChildItem $SourcePath
    if ($sourceItems){
        if (!$Force){
            throw "$Sourcepath is not empty. Please choose another path. Or use the Force parameter"
        }
        else{
            $sourceItems|Remove-Item -Force -Recurse
        }
    }
    if (!$GitHubUserName){
        throw "GitHubUserName parameter is null. Consider adding a default value for this variable to your profile.ps1"
    }
    if (!$GitHubToken){
        throw "GitHubToken parameter is null. Consider adding a default value for this variable to your profile.ps1"
    }
    
    git clone "https://$GitHubUserName`:$GitHubToken@github.com/eXpandFramework/Reactive.XAF.git" --branch $Branch
    Push-Location .\Reactive.XAF
}
else{
    Push-Location $SourcePath
}

Clear-NugetCache XpandPackages #clear xpand packages so only one version will be in cache after restoration


dotnet tool restore #ensure tools e.g paket
Move-PaketSource 0 $DXApiFeed

Write-HostFormatted "Restore all packages" -section
Invoke-PaketRestore
Write-HostFormatted "Sing Hangfire" -section
& powershell.exe ".\build\targets\Xpand.XAF.Modules.JobScheduler.Hangfire.ps1" -nugetPackagesFolder "$env:USERPROFILE\.nuget\packages"
Push-Location .\src\extensions
Write-HostFormatted "Build Extensions" -section
Start-Build
Pop-Location
Write-HostFormatted "Build modules" -section
Push-Location .\src\modules
Start-Build
Pop-Location
Pop-Location


    
