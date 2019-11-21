$local="C:\Work\eXpandFramework\XpandPwsh\XpandPwsh\Public\System\Write-HostFormatted.ps1"
if (Test-Path $local){
    . $local
}
else{
    "ConvertTo-FramedText.ps1","Write-HostFormatted.ps1"|ForEach-Object{
        $c=[System.Net.WebClient]::new()
        $file="$PSScriptRoot\$_"
        Remove-Item $file -Force -ErrorAction SilentlyContinue
        $c.DownloadFile("https://raw.githubusercontent.com/eXpandFramework/XpandPwsh/master/XpandPwsh/Public/System/$_",$file)
        . $file
    }
    
}