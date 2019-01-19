param($gist = "https://gist.githubusercontent.com/apobekiaris/6c576c87106477b42ea39de121726cd5/raw/5af69b353eee3afa516327e1ad4c49be73c35b49/XpandPosh.psm1")
$webclient = New-Object System.Net.WebClient
$fileName="$([System.io.path]::GetTempPath())\XpandPosh.psm1"
Set-Content $fileName $webclient.DownloadString($gist)
$webclient.Dispose()
Import-Module $fileName -Force -Global