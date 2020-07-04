param(
    $GithubToken=$env:GitHubToken
)
$ErrorActionPreference = "Stop"
Write-Host "Download office credential" 
if ($GithubToken){
    Remove-Item $env:TEMP\storage -Force -Recurse -ErrorAction SilentlyContinue
    Set-Location $env:TEMP
    git clone "https://apobekiaris:$GithubToken@github.com/eXpandFramework/storage.git"
    Set-Location $env:TEMP\storage\Azure
    if (!(Test-path "$PSScriptRoot\..\bin\Tests")){
        New-Item "$PSScriptRoot\..\bin\Tests" -ItemType Directory
    }
    
    "AzureAppCredentials.json","AuthenticationDataWin.json","AuthenticationDataWeb.json","dxmailpass.json"|ForEach-Object{
        Copy-Item -Destination "$PSScriptRoot\..\bin" -Force -Path ".\$_"
        Copy-Item -Destination "$PSScriptRoot\..\bin\Tests" -Force -Path ".\$_"
    }
}
