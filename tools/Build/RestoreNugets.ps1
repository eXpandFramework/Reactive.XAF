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
function UpdateHintPaths($xml,$filename) {

    $xml.Project.ItemGroup.Reference|Where-Object{"$($_.Include)".StartsWith("DevExpress.XAF")}|ForEach-Object -ErrorAction  Stop    {
        $include=$_.Include
        $index=$include.indexOf(",")
        $referenceName=$_.Include
        if ($index -gt -1){
            $referenceName=$_.Include.Substring(0,$index)
        }
        $_.Include=$referenceName
        $assemblyPath="$rootLocation\bin\"
        $hintPath=GetRelativePath $filename $assemblyPath
        $_.HintPath="$hintPath"+"$referenceName.dll"
    }
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
    UpdateHintPaths $projXml $fileName
    RemoveLicxFiles $projXml
    $projXml.Save($fileName)
} 
