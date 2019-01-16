$ErrorActionPreference = "Stop"
$rootLocation="$PSScriptRoot\..\..\"
Set-Location $rootLocation
function UpdateLanguageVersion($xml) {
    $xml.Project.PropertyGroup|ForEach-Object {
        if (!$_.LangVersion) {
            $_.AppendChild($_.OwnerDocument.CreateElement("LangVersion", $xml.DocumentElement.NamespaceURI))
        }
        $_.LangVersion = "latest"
    }
}
function SetDebugSymbols($xml) {
    $xml.Project.PropertyGroup|ForEach-Object {
        if (!$_.DebugSymbols) {
            $_.AppendChild($_.OwnerDocument.CreateElement("DebugSymbols", $xml.DocumentElement.NamespaceURI))
        }
        $_.DebugSymbols = "true"
    }
    $xml.Project.PropertyGroup|ForEach-Object {
        if (!$_.DebugType) {
            $_.AppendChild($_.OwnerDocument.CreateElement("DebugType", $xml.DocumentElement.NamespaceURI))
        }
        $_.DebugType = "full"
    }
}

function SignAssembly($xml, $fileName) {
    $xml.Project.PropertyGroup|ForEach-Object {
        if (!$_.SignAssembly) {
            $_.AppendChild($_.OwnerDocument.CreateElement("SignAssembly", $xml.DocumentElement.NamespaceURI))
        }
        $_.SignAssembly = "true"
        if (!$_.AssemblyOriginatorKeyFile) {
            $_.AppendChild($_.OwnerDocument.CreateElement("AssemblyOriginatorKeyFile", $xml.DocumentElement.NamespaceURI))
        }
        $snk="$rootLocation\src\Xpand.key\xpand.snk"
        $path=GetRelativePath $fileName $snk
        $_.AssemblyOriginatorKeyFile="$path"
    }
}

function GetRelativePath($fileName,$other) {
    $location=Get-Location
    Set-Location $((get-item $filename).DirectoryName)
    $path=Resolve-Path $other -Relative
    Set-Location $location
    return $path
}

function UpdateXAFNugetReferences($xml,$filename) {
     $xml.Project.ItemGroup.Reference|Where-Object{$_.Include }|ForEach-Object{
         $referenceName=GetReferenceName $_
         if ($referenceName.StartsWith("Xpand.XAF")){
             $_.Include=$referenceName
             UpdateHintPath $_ $fileName
         }
     }
}

function UpdateHintPath($reference,$filename){
    $hintPath=GetRelativePath $filename "$rootLocation\bin\"
    if (!$reference.HintPath){
        $reference.AppendChild($reference.OwnerDocument.CreateElement("HintPath", $xml.DocumentElement.NamespaceURI))
    }
    $reference.HintPath="$hintPath\$($reference.Include).dll"
}

function UpdateDevExpressReferences($xml,$filename) {
     $xml.Project.ItemGroup.Reference|Where-Object{$_.Include -like "DevExpress.*" }|ForEach-Object{
        $_.Include=[System.Text.RegularExpressions.Regex]::replace($_.Include, ",.*", "")
        UpdateHintPath $_ $fileName
     }
}

function GetReferenceName($item){
    $comma=$item.Include.indexOf(",")
    $referenceName=$item.Include
    if ($comma -gt -1){
        $referenceName=$item.Include.Substring(0,$comma)
    }
    return $referenceName
}

function RemoveLicxFiles($xml){
    $xml.Project.ItemGroup.EmbeddedResource|ForEach-Object{
        if ($_.Include -eq "Properties\licenses.licx"){
            $_.parentnode.RemoveChild($_)|out-null
        }
    }
}

Get-ChildItem -Filter *.csproj -Recurse |  ForEach-Object {
    $fileName = $_.FullName
    [xml]$projXml = Get-Content $fileName
    SignAssembly $projXml $fileName
    UpdateLanguageVersion $projXml
    SetDebugSymbols $projXml
    UpdateXAFNugetReferences $projXml $fileName
    UpdateDevExpressReferences $projXml $fileName
    RemoveLicxFiles $projXml
    $projXml.Save($fileName)
} 
