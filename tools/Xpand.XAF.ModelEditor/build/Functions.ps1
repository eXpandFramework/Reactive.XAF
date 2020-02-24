. "$PSScriptRoot\Common.ps1"
function DownloadDependencies($dxVersion) {
    Write-VerboseLog "Downloading Model Editor dependencies..."
    Push-Location $PSScriptRoot
    $projFile = "$PSScriptRoot\ModelerLibDownloader\ModellerLibDownloader.csproj"
    [xml]$proj = Get-Content $projFile
    $proj.Project.ItemGroup.PackageReference | Where-Object { $_.Include -like "DevExpress*" } | ForEach-Object {
        $_.Version = "$dxVersion"
    }
    $proj.Save($projFile)
    $outputDir = ".\ModelerLibDownloader\bin\$dxVersion"
    $buildResult = dotnet build ".\ModelerLibDownloader" /v:m -o $outputDir
    if ($LASTEXITCODE) {
        throw $buildResult
    }
    "exe", "pdb" | ForEach-Object {
        Copy-Item ".\Xpand.XAF.ModelEditor.$_" $outputDir
    }
}

function CreateModelEditorBootStrapper {
    param(
        [parameter(Mandatory)]
        [string]$SolutionPath,
        [parameter(Mandatory)] 
        [version]$dxVersion,
        [parameter(Mandatory)]
        [string]$OutputPath
    )
    $solutionDir = (Get-Item $SolutionPath).DirectoryName
    Write-Verbose "Create BootStrapper for all projects in $SolutionPath"
    GetSolutionProjects $SolutionPath | ForEach-Object {
        $projectPath = "$solutionDir\$_"
        [xml]$proj = Get-Content $projectPath
        $outputType = GetOutputType $proj
        $projectDirectory = (Get-Item $ProjectPath).DirectoryName
        $isFronEndProject = ($OutputType -in "exe", "winexe") -or (Test-Path "$projectDirectory\web.config")
        $assemblyPath = GetAssemblypath $projectPath $OutputPath
        $modelFileName = ($proj.Project.ItemGroup.Content | Where-Object { $_.Include -like "*.xafml" }).Include
        if (!$isFronEndProject) {
            $modelFileName = ($proj.Project.ItemGroup.EmbeddedResource | Where-Object { $_.Include -like "*.xafml" }).Include
        }
        $modelPath = "$projectDirectory\$modelFileName"
        $content = "start /d `"$PSScriptRoot\ModelerLibDownloader\bin\$dxVersion`" Xpand.XAF.ModelEditor.exe $isFronEndProject `"$assemblyPath`" `"$modelPath`" `"$projectDirectory`"" 
        Set-Content -path "$projectDirectory\Xpand.XAF.ModelEditor.bat" -value $content
        Write-Verbose "Updating $projectDirectory\Xpand.XAF.ModelEditor.bat"
    }
}

function GetAssemblyName {
    param (
        [parameter(Mandatory)]
        [xml]$proj,
        [parameter(Mandatory)]
        $ProjectPath
    )
    $assemblyName = $proj.Project.PropertyGroup.AssemblyName | Select-Object -First 1
    if (!$assemblyName) {
        $assemblyName = (Get-Item $projectPath).BaseName
    }
    $assemblyName
}

function GetSolutionProjects {
    param (
        $SolutionPath
    )
    $resultlist = New-Object System.Collections.Specialized.StringCollection
    $regex = [regex] '(?isn)Project\([^)]*\)[^,]*[^"]*"(?<path>[^"]*)'
    $match = $regex.Match((Get-Content $SolutionPath -Raw))
    while ($match.Success) {
        $resultlist.Add($match.Groups['path'].Value) | Out-Null
        $match = $match.NextMatch()
    } 
    $resultlist
}

function CreateExternalIDETool ($SolutionPath, $OutputType, $OutputPath, $AssemblyName) {
    $solutionItem = Get-Item $SolutionPath
    if (Test-Path "$($solutionItem.DirectoryName)\.idea") {
        $solutionSettings = "$($solutionItem.DirectoryName)\$($SolutionItem.Name).DotSettings"
        $execureModelerCommand = '<s:String x:Key="/Default/CustomTools/CustomToolsData/@EntryValue">ExternalToolData|Xpand.XAF.ModelEditor||xafml|C:\Windows\System32\cmd.exe|/Q /D /E:OFF /C "$PROJECT_FOLDER$\Xpand.XAF.ModelEditor.bat"#ExternalToolData|Cmd||bat|C:\Windows\System32\cmd.exe|"$FILE$"</s:String>'
        if (!(Test-Path $solutionSettings)) {
            Write-VerboseLog "Creating New $($SolutionItem.Name).DotSettings"
            $xml = @"
        <wpf:ResourceDictionary xml:space="preserve" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:s="clr-namespace:System;assembly=mscorlib" xmlns:ss="urn:shemas-jetbrains-com:settings-storage-xaml" xmlns:wpf="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            $execureModelerCommand
        </wpf:ResourceDictionary>
"@
            Set-Content $solutionSettings $xml
        }
        else {
            Write-VerboseLog "Modyfing $($SolutionItem.Name).DotSettings"
            $ns = ([xml](Get-Content $solutionSettings)).DocumentElement.NamespaceURI
            [xml]$settingsXml = ( Select-Xml -Path $solutionSettings -XPath / -Namespace @{mse = $ns }).Node
            if ($settingsXml.ResourceDictionary.innerXml -notlike "*Xpand.XAF.ModelEditor*") {
                $settingsXml.ResourceDictionary.innerXml += $execureModelerCommand
            }
            $settingsXml.Save($solutionSettings)
        }
    }
}

function GetAssemblyPath {
    param (
        [parameter(Mandatory)]
        [string]$projectPath,
        [parameter(Mandatory)]
        [string]$OutputPath
    )
    [xml]$proj = Get-Content $projectPath
    $projectDirectory = (Get-Item $ProjectPath).DirectoryName
    $assemblyExtension = GetAssemblyExtension (GetOutputType $proj)
    $assemblyName=GetAssemblyName $proj $ProjectPath
    [System.IO.Path]::GetFullPath("$projectDirectory\$OutputPath\$assemblyName.$assemblyExtension")
}

function GetOutputType {
    param (
        [parameter(Mandatory)]
        [xml]$proj
    )
    $proj.Project.PropertyGroup.OutputType | Select-Object -First 1
}
function GetAssemblyExtension {
    param (
        [parameter(Mandatory)]
        [string]$OutputType
    )

    $assemblyExtension = "dll"
    if ($OutputType -in "exe", "winexe") {
        $assemblyExtension = "exe"
    }
    $assemblyExtension
    
}