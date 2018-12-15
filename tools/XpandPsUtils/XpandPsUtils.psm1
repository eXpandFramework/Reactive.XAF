
function Install-Chocolatey {
    Invoke-Expression ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))
}
function Restore-ProjectPackages{
    $nuget="$PSScriptRoot\..\nuget.exe"
    $project= get-project
    "Resstoring packages in $($project.FullName)"
    & $nuget restore $project.FullName
}
function Install-Nuget() {
    Install-Chocolatey
    cinst NuGet.CommandLine
}

function Clear-ASPNETTemp() {
    Get-ChildItem "C:\Windows\Microsoft.NET\Framework*\v*\Temporary ASP.NET Files" -Recurse | Remove-Item -Recurse
}

function Restart-XpandPsUtils() {
    Import-Module XpandPsUtils -Force
}

function Remove-Nuget($id) {
    Get-ChildItem -Recurse -Filter '*.csproj' | ForEach-Object { $_.FullName } | False-XpandSpecificVersions
    CleanProjectCore
    Get-ChildItem -Recurse -Filter 'packages.config' | ForEach-Object { $_.FullName } | Write-XmlComment -xPath "//package[contains(@id,'$id')]"

    Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer } | Where-Object { $_.Name -eq 'packages' } | ForEach-Object {
  		    Push-Location $_
  		    Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer } |  Where-Object { $_.Name -like "$id*" } | Remove-Item -Recurse
    }        
}

function Clear-ProjectDirectories() {
    CleanProjectCore
    Get-ChildItem -Include "*.log", "*.bak" -Recurse | Remove-Item -Force
    $folders = "packages"
    Get-ChildItem -Recurse | 
        Where-Object { $_.PSIsContainer} | 
        Where-Object { $folders -contains $_.Name   } | 
        Remove-Item -Force -Recurse
}

function CleanProjectCore() {
    Get-ChildItem -Recurse | 
        Where-Object { $_.PSIsContainer } | 
        Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'obj' -or $_.Name -eq '.vs' -or $_.Name.StartsWith('_ReSharper')} | foreach{
            try {
                (Get-Item $_.FullName).Delete($true)
            }
            catch {
                $_
            }
        }
        
        
}

function Update-PackageInProjects($projectWildCard, $packageId, $version) {
    Get-Project $projectWildCard | ForEach-Object {Update-Package  -Id $packageId -ProjectName $_.ProjectName -Version $version}
}

function Install-AllProjects($packageName) {
    Get-Project -All | Install-Package $packageName
}

function Uninstall-ProjectAllPackages($packageFilter) {
    
    while((Get-Project | Get-Package | where  {
        $_.id.Contains($packageFilter)
    } ).Length -gt 0) { 
        Get-Project | Get-Package | where  {
            $_.id.Contains($packageFilter)
        } | Uninstall-Package 
    }
    
}
function Uninstall-ProjectXAFPackages {
    Uninstall-ProjectAllPackages DevExpress.XAF.
}

Function Start-UnZip {
    [cmdletbinding()]
    Param (
        [parameter(ValueFromPipeline = $True, mandatory = $True)]
        [string]$fileName,
        [string]$dir
    )
    Begin {
        Write-Verbose "Initialize stuff in Begin block"
    }

    Process {
        Add-Type -Assembly System.IO.Compression.FileSystem
        $targetDir = $dir
        if ($dir -eq "") {
            $targetDir = Split-Path $fileName
        }
        Write-Verbose "Unzipping $fileName into $targetDir"
        [System.IO.Compression.ZipFile]::ExtractToDirectory($fileName, $targetDir)
    }

    End {
        Write-Verbose "Final work in End block"
    }
}

Function Start-Zip {
    [cmdletbinding()]
    Param (
        [parameter(ValueFromPipeline = $True, mandatory = $True)]
        [string]$fileName,
        [string]$dir,
        [System.IO.Compression.CompressionLevel]$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal,
        [bool]$deleteAfterArchiving
    )
    Begin {
        Write-Verbose "Initialize stuff in Begin block"
    }

    Process {
        Add-Type -Assembly System.IO.Compression.FileSystem
        $tempDir = [System.IO.Path]::GetTempPath()
        $targetDir = $dir
        if ($dir -eq "") {
            $targetDir = Split-Path $fileName
        }
        $fileNameWithoutPath = [io.path]::GetFileName($fileName)
        $tempFileName = [io.path]::Combine($tempDir, $fileNameWithoutPath)
        Write-Verbose "Zipping $fileName into $tempDir"
        [System.IO.Compression.ZipFile]::CreateFromDirectory($targetDir, $tempFilename, $compressionLevel, $false)
        Copy-Item -Force -Path $tempFileName -Destination $fileName
        Remove-Item -Force -Path $tempFileName
        if ($deleteAfterArchiving) {
            Get-ChildItem -Path $targetDir -Exclude $fileNameWithoutPath -Recurse | Select-Object -ExpandProperty FullName | Remove-Item -Force -Recurse
        }
    }

    End {
        Write-Verbose "Final work in End block"
    }
}

function Start-ZipProject() {
    Get-ChildItem -Recurse | 
        Where-Object { $_.PSIsContainer } | 
        Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'packages' -or $_.Name -eq 'obj' -or $_.Name -eq '.vs' -or $_.Name.StartsWith('_ReSharper')} | 
        Remove-Item -Force -Recurse 
    
    $zipFileName = [System.IO.Path]::Combine($currentLocation, $zipFileName)
    Start-Zip -fileName $zipFileName 
}

function ZipFiles( $zipfilename, $sourcedir ) {
    Add-Type -Assembly System.IO.Compression.FileSystem
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcedir,
        $zipfilename, $compressionLevel, $false)
}

Function Write-XmlComment {

    [cmdletbinding()]
  
    Param (
        [parameter(ValueFromPipeline = $True, mandatory = $True)]
        [string]$fiLeName,
        [parameter(mandatory = $True)]
        [string]$xPath
  
    )
  
    Process {
        $xml = [xml](Get-Content $fiLeName)
        $xml.SelectNodes($xPath)| ForEach-Object { 
            $abc = $_;
            $comment = $xml.CreateComment($abc.OuterXml);
            $abc.ParentNode.ReplaceChild($comment, $abc);
        }
        $xml.Save($fiLeName)
    }
  
}

Export-ModuleMember -function Clear-ASPNETTemp
Export-ModuleMember -function Clear-ProjectDirectories
Export-ModuleMember -function Install-Chocolatey
Export-ModuleMember -function Install-AllProjects
Export-ModuleMember -function Install-Nuget
Export-ModuleMember -function Remove-Nuget
Export-ModuleMember -function Restart-XpandPsUtils
Export-ModuleMember -function Start-UnZip
Export-ModuleMember -function Start-Zip
Export-ModuleMember -function Uninstall-ProjectAllPackages
Export-ModuleMember -function Uninstall-ProjectXAFPackages
Export-ModuleMember -function Update-PackageInProjects
Export-ModuleMember -function Restore-ProjectPackages