param(
    $GithubToken=$env:GitHubToken
)
$ErrorActionPreference = "Stop"
Write-Host "Download office credential" 
if ($GithubToken){
    Remove-Item $env:TEMP\storage -Force -Recurse -ErrorAction SilentlyContinue
    Set-Location $env:TEMP
    git clone "https://apobekiaris:$GithubToken@github.com/eXpandFramework/storage.git"
    if (!(Test-path "$PSScriptRoot\..\bin\Tests")){
        New-Item "$PSScriptRoot\..\bin\Tests" -ItemType Directory
    }
    Set-Location $env:TEMP\storage\Azure
    "MicrosoftAppCredentials.json","MicrosoftAuthenticationDataWin.json","MicrosoftAuthenticationDataWeb.json","dxmailpass.json"|ForEach-Object{
        Copy-Item -Destination "$PSScriptRoot\..\bin" -Force -Path ".\$_"
        Copy-Item -Destination "$PSScriptRoot\..\bin\Tests" -Force -Path ".\$_"
        Copy-Item -Destination "$PSScriptRoot\..\src\Tests\ALL\TestApplication" -Force -Path ".\$_"
    }
    Set-Location $env:TEMP\storage\Google
    @("GoogleWinAppCredentials.json","GoogleWebAppCredentials.json","GoogleAuthenticationDataWin.json","GoogleAuthenticationDataWeb.json","TestAppPass.json","WinAuth.exe","Xpand.testapplication.xml")|ForEach-Object{
        Copy-Item -Destination "$PSScriptRoot\..\bin" -Force -Path ".\$_"
        Copy-Item -Destination "$PSScriptRoot\..\bin\Tests" -Force -Path ".\$_"
        Copy-Item -Destination "$PSScriptRoot\..\src\Tests\ALL\TestApplication" -Force -Path ".\$_"
    }
}
