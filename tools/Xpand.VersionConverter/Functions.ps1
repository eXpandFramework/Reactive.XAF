. "$PSScriptRoot\Common.ps1"
function Resolve-Refs{
    param(
        $projectFile,
        $targetFilter,
        $referenceFilter
    )
    $dirBuld="$((Get-Item $projectFile).DirectoryName)\Directory.build.targets"
    if (!(Test-Path $dirBuld)){
        $xml=@"
<Project>
    <Target Name="PrintReferences" DependsOnTargets="ResolveProjectReferences;ResolveAssemblyReferences">
	  <Message Text="@(_ResolveAssemblyReferenceResolvedFiles)" />
    </Target>
</Project>
"@
        Set-Content $dirBuld  $xml
        $removeTargets=$true
    }
    else{
        $target=@"
    <Target Name="PrintReferences" DependsOnTargets="ResolveProjectReferences;ResolveAssemblyReferences">
	  <Message Text="@(_ResolveAssemblyReferenceResolvedFiles)" />
    </Target>
</Project>
"@
        $xml=Get-Content $dirBuld
        if (!($xml|select-string "PrintReferences")){
            Set-Content $dirBuld $xml.Replace("</Project>",$target)
        }
    }

    $referenceAssemblies=(& "$msbuild" $projectFile "/t:PrintReferences" /nologo).Split(';')|
        Where-Object{
            try {
                Test-Path $_ 
            }
            catch {
                
            }
        }|
        Get-item
    if ($removeTargets){
        Remove-Item $dirBuld
    }
    $referenceAssemblies

}
function Get-UnPatchedPackages {
    param(
       $assemblyList,
       $dxVersion,
       $targetFilter
    )
    $unpatchedPackages=$assemblyList|Where-Object{$_.BaseName -match $targetFilter}|ForEach-Object{
        $path="$($_.Directory.Parent.Parent.FullName)\$($_.BaseName).nuspec"
        [xml]$nuspec=Get-Content $Path
        $currentDxVersion=($nuspec.package.metadata.tags.Split(",")|select-object -last 1).Trim()
        if ($currentDxVersion -ne $dxVersion){
            $_
        }
    }
    Write-VerboseLog "unpatchedPackages:"
    $unpatchedPackages | Write-VerboseLog
    $unpatchedPackages
}
function Get-InstalledPackages {
    param(
        $projectFile,
        $assemblyFilter
    )
    [xml]$csproj = Get-Content $projectFile
    $packagesFolder = Get-packagesfolder
    
    [array]$packageReferences = $csproj.Project.ItemGroup.PackageReference | ForEach-Object {
        if ($_.Include -like "$assemblyFilter") {
            [PSCustomObject]@{
                Id      = $_.Include
                Version = $_.Version
            }
        }
    }
    
    [array]$dependencies = $packageReferences | ForEach-Object { Get-PackageDependencies $_ $packagesFolder $assemblyFilter $projectFile }
    $dependencies + $packageReferences
}

function Get-PackageDependencies {
    [CmdletBinding()]
    param (
        [parameter(ValueFromPipeline)]
        $psObj,
        $packagesFolder,
        $assemblyFilter,
        $projectFile
    )
    
    begin {
    }
    
    process {
        $nuspecPath = "$packagesFolder\$($psObj.Id)\$($psObj.Version)\$($psObj.Id).nuspec"
        if (!(Test-Path $nuspecPath)) {
            Restore-Packages $projectFile
            if (!(Test-Path $nuspecPath)) {
                throw "$nuspecPath not found."
            }
        }
        [xml]$nuspec = Get-Content $nuspecPath
        
        [array]$packages = $nuspec.package.metadata.dependencies.group.dependency | Where-Object { $_.id -like "$assemblyFilter" } | ForEach-Object {
            [PSCustomObject]@{
                Id      = $_.Id
                Version = $_.Version
            }
        } 
        
        [array]$dependencies = $packages | ForEach-Object { Get-PackageDependencies $_ $packagesFolder $assemblyFilter $projectFile }
        $dependencies + $packages
        
    }

    end {
    }
}

function Restore-Packages {
    $nuget = "$PSScriptRoot\nuget.exe"
    if (!(Test-Path $nuget)) {
        $c = [System.Net.WebClient]::new()
        $c.DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", $nuget)
        $c.dispose()
    }
    & $nuget Restore $projectFile | Out-Null
}
function Get-PackagesFolder {
    $packagesFolder = "$PSSCriptRoot\..\..\.."
    if ((Get-Item "$PSScriptRoot\..").BaseName -like "Xpand.VersionConverter*") {
        $packagesFolder = "$PSSCriptRoot\..\.."
    }
    $packagesFolder
}

function Remove-PatchFlags {
    param(
        $PackageDir,
        $DXVersion
    )
    Get-ChildItem $packageDir *VersionConverter.v.* | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
}


function Get-PaketReferences {
    [CmdletBinding()]
    param (
        [System.IO.FileInfo]$projectFile = "."
    )
    
    begin {
        
    }
    
    process {
        $paketDirectoryInfo = $projectFile.Directory
        $paketReferencesFile = "$($paketDirectoryInfo.FullName)\paket.references"
        if (Test-Path $paketReferencesFile) {
            Push-Location $projectFile.DirectoryName
            $dependencies = dotnet paket show-installed-packages --project $projectFile.FullName --all --silent | ForEach-Object {
                $parts = $_.split(" ")
                [PSCustomObject]@{
                    Include = $parts[1]
                    Version = $parts[3]
                }
            }
            Pop-Location
            $c = Get-Content $paketReferencesFile | ForEach-Object {
                $ref = $_
                $d = $dependencies | Where-Object {
                    $ref -eq $_.Include
                }
                $d
            }
            Write-Output $c
        }
    }
    
    end {
        
    }
}

function Get-PaketDependenciesPath {
    [CmdletBinding()]
    param (
        [string]$Path = "."
    )
    
    begin {
        
    }
    
    process {
        $paketDirectoryInfo = (Get-Item $Path).Directory
        if (!$paketDirectoryInfo) {
            $paketDirectoryInfo = Get-Item $Path
        }
        $paketDependeciesFile = "$($paketDirectoryInfo.FullName)\paket.dependencies"
        while (!(Test-Path $paketDependeciesFile)) {
            $paketDirectoryInfo = $paketDirectoryInfo.Parent
            if (!$paketDirectoryInfo) {
                return
            }
            $paketDependeciesFile = "$($paketDirectoryInfo.FullName)\paket.dependencies"
        }
        $item = Get-Item $paketDependeciesFile
        Set-Location $item.Directory.Parent.FullName
        $item
    }
    
    end {
        
    }
}


function Write-Intent {
    [CmdletBinding()]
    param (
        [parameter(ValueFromPipeline)]
        [string]$Text,
        [int]$Level
    )
    
    begin {
        
    }
    
    process {
        for ($i = 0; $i -lt $Level.Count; $i++) {
            $prefix += "  "    
        }
        $prefix += $text
        Write-Output $prefix       
    }
    
    end {
        
    }
}

function Get-SymbolSources {
    [CmdletBinding()]
    param (
        [parameter(Mandatory, ValueFromPipeline)]
        [System.IO.FileInfo]$pdb,
        [parameter()]
        [string]$dbgToolsPath = "$PSScriptRoot\srcsrv"
    )
    
    begin {
        if (!(Test-Path $dbgToolsPath)) {
            throw "srcsrv is invalid"
        }
    }
    
    process {
        & "$dbgToolsPath\srctool.exe" $pdb
    }
    
    end {            
    }
}

function Update-Symbols {
    [CmdletBinding()]
    param (
        [parameter(Mandatory, ValueFromPipeline)]
        [System.IO.FileInfo]$pdb,
        [parameter(Mandatory, ParameterSetName = "Default")]
        [string]$TargetRoot,
        [parameter(ParameterSetName = "Default")]
        [string]$SourcesRoot,
        [parameter(ParameterSetName = "Sources")]
        [string[]]$symbolSources,
        [parameter()]
        [string]$dbgToolsPath = "$PSScriptRoot\srcsrv"
    )
    
    begin {
        if (!(Test-Path $dbgToolsPath)) {
            throw "srcsrv is invalid"
        }
        if ($PSCmdlet.ParameterSetName -eq "Default") {
            $remoteTarget = ($TargetRoot -like "http*")
        }
        else {
            $remoteTarget = $symbolSources | Where-Object { $_ -match "trg: http*" } | Select-Object -First 1
        }
        if (!$remoteTarget ) {
            if (!$SourcesRoot.EndsWith("\")) {
                $SourcesRoot += "\"
            }
            if (!$TargetRoot.EndsWith("\")) {
                $TargetRoot += "\"
            }
        }
        $list = New-Object System.Collections.ArrayList
        $pdbstrPath = "$dbgToolsPath\pdbstr.exe"
    }
    
    process {
        $list.Add($pdb) | Out-Null
    }
    
    end {
        Write-VerboseLog "Indexing $($list.count) pdb files"
        # $list | Invoke-Parallel -ActivityName Indexing -VariablesToImport @("dbgToolsPath", "TargetRoot", "SourcesRoot", "remoteTarget") -Script {
        $list | foreach {
            Write-Host "Indexing $($_.FullName) ..."
            $streamPath = [System.IO.Path]::GetTempFileName()
            Write-VerboseLog "Preparing stream header section..."
            Add-Content -value "SRCSRV: ini ------------------------------------------------" -path $streamPath
            Add-Content -value "VERSION=1" -path $streamPath
            Add-Content -value "INDEXVERSION=2" -path $streamPath
            Add-Content -value "VERCTL=Archive" -path $streamPath
            Add-Content -value ("DATETIME=" + ([System.DateTime]::Now)) -path $streamPath
            Write-VerboseLog "Preparing stream variables section..."
            Add-Content -value "SRCSRV: variables ------------------------------------------" -path $streamPath
            if ($remoteTarget) {
                Add-Content -value "SRCSRVVERCTRL=http" -path $streamPath
            }
            Add-Content -value "SRCSRVTRG=%var2%" -path $streamPath
            Add-Content -value "SRCSRVCMD=" -path $streamPath
            Write-VerboseLog "Preparing stream source files section..."
            Add-Content -value "SRCSRV: source files ---------------------------------------" -path $streamPath
            if ($symbolSources) {
                $symbolSources | ForEach-Object {
                    $regex = [regex] '(?i)\[([^\]]*)] trg: (.*)'
                    $m = $regex.Match($_)
                    $src = $m.Groups[1].Value
                    $trg = $m.Groups[2].Value
                    if ($src -and $trg) {
                        $result = "$src*$trg";
                        Add-Content -value $result -path $streamPath
                        Write-VerboseLog "Indexing to $result"
                    }
                }
            }
            else {
                $sources = & "$dbgToolsPath\srctool.exe" -r $_.FullName | Select-Object -SkipLast 1
                if ($sources) {
                    foreach ($src in $sources) {
                        $target = "$src*$TargetRoot$src"
                        if ($remoteTarget) {
                            $file = "$src".replace($SourcesRoot, '').Trim("\").replace("\", "/")
                            $target = "$src*$TargetRoot/$file"
                        }
                        Add-Content -value $target -path $streamPath
                        Write-VerboseLog "Indexing $src to $target"
                    }
                }
                else {
                    Write-Host "No steppable code in pdb file $_" -f Red
                }       
            }
            Add-Content -value "SRCSRV: end ------------------------------------------------" -path $streamPath
            Write-VerboseLog "Saving the generated stream into the $_ file..."
            & $pdbstrPath -w -s:srcsrv "-p:$($_.Fullname)" "-i:$streamPath"
            Remove-Item $streamPath
        }
    }
}


function AddXmlElement {
    [CmdletBinding(DefaultParameterSetName = "Parent")]
    param (
        [parameter(Mandatory, Position = 0)]
        [System.Xml.XmlDocument]$Owner,
        [parameter(Mandatory, Position = 1)]
        [string]$ElementName,
        [parameter(Mandatory, ParameterSetName = "Parent", Position = 2)]
        [string]$Parent,
        [parameter(Position = 3)]
        [System.Collections.IDictionary]$Attributes,
        [parameter(Position = 4)]
        [string]$InnerText,
        [parameter(Mandatory, ParameterSetName = "ParentNode", Position = 5)]
        [System.Xml.XmlElement]$ParentNode
    )
    
    begin {

    }
    
    process {
        $ns = New-Object System.Xml.XmlNamespaceManager($Owner.NameTable)
        $nsUri = $Owner.DocumentElement.NamespaceURI
        $ns.AddNamespace("ns", $nsUri)
        if ($Attributes) {
            $attributesFilter="["
            $attributesFilter=$Attributes.Keys | ForEach-Object {
                "@$_='$($Attributes[$_])'"
            } |Join-String -Separator " and "
            $attributesFilter="[$attributesFilter]"
        }
        
        $element = $Owner.SelectSingleNode("//ns:$ElementName$($attributesFilter)", $ns)
        if (($ParentNode -and $element.ParentNode -ne $ParentNode) -or ($Parent -and $element.ParentNode.LocalName -ne $Parent)){
            $element = $Owner.CreateElement($ElementName, $nsUri)
        }
        if ($Attributes) {
            $Attributes.Keys | ForEach-Object {
                $element.SetAttribute($_, $Attributes[$_])
            }
        }
        if ($InnerText){
            $element.InnerText = $InnerText;
        }
        
        if (!$ParentNode) {
            $parentNode = $Owner.SelectSingleNode("//ns:$Parent", $ns)
        }
        $parentNode.AppendChild($Owner.CreateTextNode([System.Environment]::NewLine)) | Out-Null
        $parentNode.AppendChild($Owner.CreateTextNode("    ")) | Out-Null
        
        $parentNode.AppendChild($element) | Out-Null
        $element
    }
    
    end {
        
    }
}

function UpdateBlazor {
    # [xml]$proj=Get-Content $ProjectFile
    # if ($proj.Project.Sdk -eq "Microsoft.NET.Sdk.Web" -and $proj.Project.PropertyGroup.TargetFramework -eq "net6.0"){
    #     $enable=$proj.Project.PropertyGroup.EnableUnsafeBinaryFormatterSerialization|Where-Object{$_}
    #     if (!$enable){   
    #         AddXmlElement $proj EnableUnsafeBinaryFormatterSerialization  PropertyGroup -InnerText "true"|Out-Null
    #         $proj.Save($projectFile)
    #     }
    # }
    
}