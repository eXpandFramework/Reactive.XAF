function GetDevExpressVersion($targetPath, $referenceFilter, $projectFile) {
    try {
        Write-Verbose "Locating DevExpress version..."
        $projectFileInfo = Get-Item $projectFile
        [xml]$csproj = Get-Content $projectFileInfo.FullName
        $packageReference = $csproj.Project.ItemGroup.PackageReference | Where-Object { $_ }
        if (!$packageReference) {
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
                Write-Verbose "$($dxAssembly.Name.Name) found from $hintpath"
                $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($hintPath).FileVersion
            }
            else {
                $dxAssemblyPath = Get-ChildItem $targetPath "$referenceFilter*.dll" | Select-Object -First 1
                if ($dxAssemblyPath) {
                    Write-Verbose "$($dxAssembly.Name.Name) found from $($dxAssemblyPath.FullName)"
                    $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssemblyPath.FullName).FileVersion
                }
                else {
                    $include = ($dxReferences | Select-Object -First 1)
                    Write-Verbose "Include=$Include"
                    $dxReference = [Regex]::Match($include, "DevExpress[^,]*", [RegexOptions]::IgnoreCase).Value
                    Write-Verbose "DxReference=$dxReference"
                    $dxAssembly = Get-ChildItem "$env:windir\Microsoft.NET\assembly\GAC_MSIL"  *.dll -Recurse | Where-Object { $_ -like "*$dxReference.dll" } | Select-Object -First 1
                    if ($dxAssembly) {
                        $version = [version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($dxAssembly.FullName).FileVersion
                    }
                }
            }
        }
        $version
    }
    catch {
        "Exception:"
        $_.Exception
        "InvocationInfo:"
        $_.InvocationInfo 
        Write-Warning "$howToVerbose`r`n"
        throw "Check output warning message"
    }
}