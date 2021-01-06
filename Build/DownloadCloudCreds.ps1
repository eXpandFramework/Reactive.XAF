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
            Copy-Item -Destination "$PSScriptRoot\..\bin\netcoreapp3.1" -Force -Path "$_" -Verbose
            Copy-Item -Destination "$PSScriptRoot\..\bin\net461" -Force -Path "$_" -Verbose
            Copy-Item -Destination "$PSScriptRoot\..\src\Tests\EasyTests\TestApplication" -Force -Path "$_" -Verbose
        }
    }
    git clone "https://apobekiaris:$GithubToken@github.com/eXpandFramework/storage.git"
    
    Set-Location $env:TEMP\storage\Azure
    "MicrosoftAppCredentials.json","MicrosoftAuthenticationDataWin.json","MicrosoftAuthenticationDataWeb.json","dxmailpass.json"|ForEach-Object{
        Copy-Item -Destination "$PSScriptRoot\..\bin" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\bin\netcoreapp3.1" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\bin\net461" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\src\Tests\EasyTests\TestApplication" -Force -Path ".\$_" -Verbose
    }
    Set-Location $env:TEMP\storage\Google
    @("GoogleWinAppCredentials.json","GoogleWebAppCredentials.json","GoogleAuthenticationDataWin.json","GoogleAuthenticationDataWeb.json","TestAppPass.json","WinAuth.png","Xpand.testapplication.xml")|ForEach-Object{
        
        if ($_ -eq "WinAuth.png"){
            $name="WinAuth.exe"
        }
        Copy-Item -Destination "$PSScriptRoot\..\bin\$name" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\bin\net461\$name" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\bin\netcoreapp3.1\$name" -Force -Path ".\$_" -Verbose
        Copy-Item -Destination "$PSScriptRoot\..\src\Tests\EasyTests\TestApplication\$name" -Force -Path ".\$_" -Verbose
        $name=$null
    }
}
