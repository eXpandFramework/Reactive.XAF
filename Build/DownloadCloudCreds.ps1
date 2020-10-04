param(
    $GithubToken=$env:GitHubToken,
    $GitUserEmail=$env:GitUserEmail,
    [switch]$SkipPushToken
)
$ErrorActionPreference = "Stop"

Write-Host "Download office credential" 
if ($GithubToken){
    Remove-Item $env:TEMP\storage -Force -Recurse -ErrorAction SilentlyContinue
    Set-Location $env:TEMP
    Set-Content ".\GitUserEmail.json" $GitUserEmail
    if (!$SkipPushToken){
        ".\GitUserEmail.json","$PSScriptRoot\PushToken.ps1","$PSScriptRoot\DownloadCloudCreds.ps1"|ForEach-Object{
            Copy-Item -Destination "$PSScriptRoot\..\bin" -Force -Path "$_" -Verbose
            Copy-Item -Destination "$PSScriptRoot\..\bin\Tests" -Force -Path "$_" -Verbose
            Copy-Item -Destination "$PSScriptRoot\..\src\Tests\ALL\TestApplication" -Force -Path "$_" -Verbose
        }
    }
    git clone "https://apobekiaris:$GithubToken@github.com/eXpandFramework/storage.git"
    if (!(Test-path "$PSScriptRoot\..\bin\Tests")){
        New-Item "$PSScriptRoot\..\bin\Tests" -ItemType Directory
    }
    Set-Location $env:TEMP\storage\Azure
    "MicrosoftAppCredentials.json","MicrosoftAuthenticationDataWin.json","MicrosoftAuthenticationDataWeb.json","dxmailpass.json"|ForEach-Object{
        Copy-Item -Destination "$PSScriptRoot\..\bin" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\bin\Tests" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\src\Tests\ALL\TestApplication" -Force -Path ".\$_" -Verbose
    }
    Set-Location $env:TEMP\storage\Google
    @("GoogleWinAppCredentials.json","GoogleWebAppCredentials.json","GoogleAuthenticationDataWin.json","GoogleAuthenticationDataWeb.json","TestAppPass.json","WinAuth.png","Xpand.testapplication.xml")|ForEach-Object{
        
        if ($_ -eq "WinAuth.png"){
            $name="WinAuth.exe"
        }
        Copy-Item -Destination "$PSScriptRoot\..\bin\$name" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\bin\Tests\$name" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\src\Tests\ALL\TestApplication\$name" -Force -Path ".\$_" -Verbose
        $name=$null
    }
}
