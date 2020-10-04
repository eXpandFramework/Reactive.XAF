param(
    $Service = "Google",
    $Token = 'pis.Auth.OAuth2.Responses.TokenResponse-0b17d99e-bde4-4b2c-b10e-2b3ddc5d093eﮙ{"access_token":"ya29.a0AfH6SMAJhgC5CQcnBbJl7DZMfIppkAV7VqjzfFNLfWoZg72fsPZzHCGs9bs8iHKhDPue_JurQnpmBpPo1whkpCtNVq_RDGoDof9idKoF9q2tG0nFLvEpe18eAqBGulq8-ucMmd6CQn-ggZA28Z0LTnTRhsV9dEWgI3E","token_type":"Bearer","expires_in":3599,"refresh_token":"1//09dNRCcFqGbaNCgYIARAAGAkSNwF-L9IraKlwajWsX6eCukV5u-6wdqAWhGW0xgBGu6JCF_Qt6MKe3pBoNQ38dN4M240Wy-OqAy8","scope":"https://www.googleapis.com/auth/tasks https://www.googleapis.com/auth/calendar.events openid https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/userinfo.profile https://www.googleapis.com/auth/userinfo.email","id_token":"eyJhbGciOiJSUzI1NiIsImtpZCI6IjVlZmZhNzZlZjMzZWNiNWUzNDZiZDUxMmQ3ZDg5YjMwZTQ3ZDhlOTgiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLCJhenAiOiIzODQ0MDgxMDU4NTYtYXIzdXVjbHVhMzU2ajcyZnRjbHIyYTMzaDB0bGg1YmouYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJhdWQiOiIzODQ0MDgxMDU4NTYtYXIzdXVjbHVhMzU2ajcyZnRjbHIyYTMzaDB0bGg1YmouYXBwcy5nb29nbGV1c2VyY29udGVudC5jb20iLCJzdWIiOiIxMDY0NTQ3MzYyNjQ1ODc5MDk3OTMiLCJlbWFpbCI6ImFwb3N0b2xpcy5iZWtpYXJpc0BnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiYXRfaGFzaCI6InRpTGRmbjVmcVpwVWxZRUlZNTh1S0EiLCJuYW1lIjoiQXBvc3RvbGlzIEJla2lhcmlzIiwicGljdHVyZSI6Imh0dHBzOi8vbGgzLmdvb2dsZXVzZXJjb250ZW50LmNvbS9hLS9BT2gxNEdoekxHREFaSFBadFFESWZaSzZEb0xNcVUzSWVoM1AzMlNVRVJ0VjZBPXM5Ni1jIiwiZ2l2ZW5fbmFtZSI6IkFwb3N0b2xpcyIsImZhbWlseV9uYW1lIjoiQmVraWFyaXMiLCJsb2NhbGUiOiJlbiIsImlhdCI6MTYwMTc1NjExMiwiZXhwIjoxNjAxNzU5NzEyfQ.pyjC5Tn7S9s2IgfSgDXRBT2fh2f9P9rp1Eu-bFC3Bld9uhzqtQ3tcKgzy-9_BcmpmAbuBCvIpt7MOfptkCMncirz-zb05rsv8MqbI76kUAQga1h_7flM7p4pS1wHqUOafdTESd24hY-OfAGa_irwpof_suMWVH94jiNnQKs691fskcKzyw8Q1Qe_eKQoZGOI9s9vohoMnJFZRAEA71KoT6w8me_xHT3wRWWK7CJ3Jdi_v4PHBsjCN1yuaUSEC_IcCoyVmG37vW2jfWMq6V0RQxPPaNKkhSu1M26owGlAmWrXyczE_aR4owE2hmcvvw909X3_TazbQ4TkPBpBaKd3nA","Issued":"2020-10-03T23:15:13.497+03:00","IssuedUtc":"2020-10-03T20:15:13.497Z"}',
    [switch]$SkipPushToken
)
$ErrorActionPreference = "continue"
function Push-Git {
    [CmdletBinding()]
    param (
        [parameter(ParameterSetName = "AddAll")]
        [switch]$AddAll,
        [parameter(ParameterSetName = "AddAll")]
        [string]$Message,
        [string]$Branch,
        [string]$Remote = "origin",
        [string]$Username,
        [string]$UserMail,
        [switch]$Force
    )
    
    begin {
    }
    
    process {
        git config core.autocrlf true
        if ($username) {
            git config user.name $userName
            if ($lastexitcode){
                throw
            }
        }
        if ($UserMail) {
            git config user.email $userMail
            if ($lastexitcode){
                throw
            }
        }
        if ($AddAll) {
            git add -A
            if ($message) {
                git commit -m $message
            }
            else {
                git commit --amend  --no-edit
            }
        }
        $a = @("-q")
        if ($Force) {
            $a += "-f"
        }
        $a += $Remote
        if ($Branch) {
            $a += $Branch
        }
        git push @a
        if ($lastexitcode){
            throw
        }
    }
    
    end {
        
    }
}
$token = Get-Content "$PSScriptRoot\$Service`Token.txt"
$VerbosePreference = "continue"

Set-Location $env:TEMP
$GitUserEmail = Get-Content .\GitUserEmail.Json 
Set-Location "$env:TEMP\storage\$Service"
if ($Service -eq "Azure") {
    $Service = "Microsoft"
}
$dataFile = ".\$Service"
$dataFile += "AuthenticationDataWin.json"
Write-Host "Write Token"
Set-Content $dataFile $Token
Start-Sleep 2
Write-Host "Push-Git"
Push-Git -AddAll -Message "$Service`Token" -UserMail $GitUserEmail -Username "apobekiaris"
Start-Sleep 2
Write-Host "DownloadCread"
& "$PSScriptRoot\DownloadCloudCreds.ps1" -SkipPushToken:$SkipPushToken
Write-Host "Finish"
Start-Sleep 2