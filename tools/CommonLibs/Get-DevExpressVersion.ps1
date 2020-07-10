function GetDevExpressVersion($targetPath, $referenceFilter, $projectFile) {
    Write-VerboseLog "Locating $referenceFilter version..."
    $projectFileInfo = Get-Item $projectFile
    [xml]$csproj = Get-Content $projectFileInfo.FullName
    $packageReference = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_ }
    if (!$packageReference) {
        Write-VerboseLog "Locating $referenceFilter version from PackageRefences..."
        $packageReference = Get-PaketReferences (Get-Item $projectFile)
    }
    $packageReference = $packageReference | Where-Object { $_.Include -like "$referenceFilter" }
    if ($packageReference) {
        $v = ($packageReference ).Version | Select-Object -First 1
        if ($packageReference) {
            $version = [version]$v
            if ($version.Revision -eq -1) {
                $v += ".0"
                $version = [version]$v
            }
        }
    }
    
    if (!$packageReference -and !$paket) {
        Write-VerboseLog "Locating $referenceFilter version...from references"
        $references = $csproj.Project.ItemGroup.Reference
        $dxReferences = $references | Where-Object { $_.Include -like "$referenceFilter" }
        $hintPath = $dxReferences.HintPath | ForEach-Object { 
            if ($_) {
                $path = $_
                if (![path]::IsPathRooted($path)) {
                    $path = "$((Get-Item $projectFile).DirectoryName)\$_"
                }
                if (Test-Path $path) {
                    [path]::GetFullPath($path)
                }
            }
        } | Where-Object { $_ } | Select-Object -First 1
        if ($hintPath ) {
            Write-VerboseLog "$($dxAssembly.Name.Name) found from $hintpath"
            $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($hintPath).FileVersion
        }
        else {
            $dxAssemblyPath = Get-ChildItem $targetPath "$referenceFilter*.dll" | Select-Object -First 1
            if ($dxAssemblyPath) {
                Write-VerboseLog "$($dxAssembly.Name.Name) found from $($dxAssemblyPath.FullName)"
                $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssemblyPath.FullName).FileVersion
            }
            else {
                $include = ($dxReferences | Select-Object -First 1).Include
                Write-VerboseLog "Include=$Include"
                $dxReference = [Regex]::Match($include, "DevExpress[^,]*", [RegexOptions]::IgnoreCase).Value
                Write-VerboseLog "DxReference=$dxReference"
                $dxAssembly = Get-ChildItem "$env:windir\Microsoft.NET\assembly\GAC_MSIL"  *.dll -Recurse | Where-Object { $_.BaseName -like $dxReference } | Select-Object -First 1
                if ($dxAssembly) {
                    $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly.FullName).FileVersion
                }
            }
        }
    }
    "$($version.Major).$($version.Minor).$($version.Build)"
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