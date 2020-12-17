Framework "4.6"
$ErrorActionPreference = "Stop"
$Global:Configuration="Debug"

Properties {
    $packageSources = $null
    $nugetBin = "$PSScriptRoot\bin\nuget\"
    $cleanBin = $null
    $nugetApiKey = $null
    $compile = $true
    $dxVersion = $null
    $branch = $null
    $CustomVersion = $false
    $Root = "$nugetbin\..\..\"
}

Task BuildNugetConsumers -depends   CreateNuspec, PackNuspec, CompileNugetConsumers
Task Build  -depends   Clean, Init, UpdateProjects, Compile,CheckVersions, IndexSources,CompileTests

function CompileTestSolution($conf){
    Write-HostFormatted "Building Tests" -Section
    New-Item -Name Nupkg -ItemType Directory -Path "$root\bin" -ErrorAction SilentlyContinue
    $solution="$Root\src\Tests\Tests.sln"
    $conf=GetConfiguration $solution $conf
    "Configuration=$conf"
    Start-Build -Path $solution -Configuration $conf -BinaryLogPath "$Root\Bin\Test$conf.binlog" -Verbosity minimal -WarnAsError 
    # Get-ChildItem "$root\bin" "DevExpress.Persistent.Base.v*.*"|Copy-Item -Destination "$root\bin\net461" -Force

    # FixWrongPersistentBaseFramework
    Start-Build -Path "$Root\src\Tests\Office.DocumentStyleManager\Xpand.XAF.Modules.Office.DocumentStyleManager.Tests.csproj"
    # Start-Build -Path $solution -Configuration FixWrongPersistentBaseFramework
    Copy-Item -path $root\bin\runtimes -Recurse -Destination $root\bin\TestWinApplication\runtimes -Container -Force
}

Task IndexSources {
    Invoke-Script {
        $sha = Get-GitLastSha "https://github.com/eXpandFramework/DevExpress.XAF" $branch
        "Xpand.XAF.Modules.*.pdb","Xpand.Extensions.*.pdb"|ForEach-Object{
            Get-ChildItem "$root\bin" $_ | Update-XSymbols -SourcesRoot "$Root" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$sha"
            Get-ChildItem "$root\bin\net461" $_ | Update-XSymbols -SourcesRoot "$Root" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$sha"
        }
        
    }
}

Task Init {
    
    Invoke-Script {
        "Create bin directory in $Root"
        New-Item "$Root\bin" -ItemType Directory -Force 
        New-Item "$Root\bin\ReactiveLoggerClient" -ItemType Directory -Force | Out-Null
        New-Item "$Root\bin\TestWebApplication" -ItemType Directory -Force | Out-Null
        
        Copy-Item -Path "$root\build\Tests.runsettings" -Destination "$Root\bin\Tests.runsettings" -Force

        dotnet tool restore
        Set-Location $root
        Invoke-Script{Invoke-PaketRestore -strict }
        
        Get-ChildItem "$(Get-NugetInstallationFolder)\grpc.core" "runtimes" -Recurse|Select-Object -Last 1|Copy-Item -Destination "$root\bin\runtimes" -Recurse -Force
    }
}

Task UpdateProjects {
    Invoke-Script {
        & "$PSScriptRoot\UpdateProjects.ps1" $DXVersion
    }
}


Task CompileTests -precondition { return $compile } {
    Invoke-Script {
        CompileTestSolution $Global:Configuration
        if (!(Test-AzDevops)){
            Invoke-Task -taskName BuildNugetConsumers
        }
    } -Maximum 3
    Get-ChildItem $root\bin "*xpand*.dll"| Test-AssemblyReference -VersionFilter $DXVersion
}

Task CompileNugetConsumers -precondition { return $compile } {
    Invoke-Script {
        $localPackages = @(& (Get-NugetPath) list -source "$root\bin\nupkg;"|ConvertTo-PackageObject|Where-Object{$_.id -like "*.ALL"} | ForEach-Object {
            $version = [version]$_.Version
            if ($version.revision -eq 0) {
                $version = New-Object System.Version ($version.Major, $version.Minor, $version.build)
            }
            [PSCustomObject]@{
                Id      = $_.Id
                Version = $version
            }
        })
        
        Write-HostFormatted "Update all package versions" -ForegroundColor Magenta
        Get-ChildItem "$root\src\Tests\EasyTests" *.csproj -Recurse|ForEach-Object{
            $prefs=Get-PackageReference $_ 
            $prefs|Where-Object{$_.include -like "Xpand.XAF.*"}|ForEach-Object{
                $ref=$_
                $localPackages|Where-Object{$_.id-eq $ref.include}|ForEach-Object{
                    $ref.version=$_.version.ToString()
                }
            }
            ($prefs|Select-Object -First 1).OwnerDocument.Save($_)
        }
        CompileTestSolution "NugetConsumers"
    } -Maximum 3
    Get-ChildItem $root\bin "*xpand*.dll"| Test-AssemblyReference -VersionFilter $DXVersion
}

function GetConfiguration($solution,$conf){
    $conf=$conf.ToLower()
    $match=(Read-MSBuildSolutionFile $solution).SolutionConfigurations.ConfigurationName|Sort-Object -Unique|ForEach-Object{
        $configVersion=$_.ToLower().Replace("$conf`_","") 
        if ((($_ -like "$conf`_*") -and (Test-Version $configVersion))){
            if (([version]$configVersion) -ge ([version](Get-VersionPart $dxVersion Minor))){
                $_
            }
            
        }
    }
    if ($match){
        $match
    }
    else{
        $conf
    }
}

Task Compile -precondition { return $compile } {
    if (($branch -eq "master") -and ((Get-DevExpressVersion) -eq $DXVersion)){
        $Global:Configuration="Release"
    }
    
    Invoke-Script {
        Write-HostFormatted "Building Extensions" -Section
        $solution="$Root\src\Extensions\Extensions.sln"
        
        $Configuration=GetConfiguration $solution $Global:Configuration
        Start-Build -Path $solution -Configuration $Configuration -BinaryLogPath "$Root\Bin\Extensions.binlog" -Verbosity minimal -WarnAsError -PropertyValue "skipNugetReplace=true"
    } -Maximum 3
    
    
    Invoke-Script {
        $solution="$Root\src\Modules\Modules.sln"
        Write-HostFormatted "Building Modules" -Section
        $Configuration=GetConfiguration $solution $Global:Configuration
        Start-Build -Path $solution -Configuration $Configuration -BinaryLogPath "$Root\Bin\Modules.binlog" -Verbosity minimal -WarnAsError -PropertyValue "skipNugetReplace=true"
    } -Maximum 3
    
    Invoke-Script {
        Write-HostFormatted "Building Xpand.XAF.ModelEditor" -Section
        dotnet msbuild "$Root\tools\Xpand.XAF.ModelEditor\Xpand.XAF.ModelEditor.csproj" /t:Clean
        dotnet restore "$Root\tools\Xpand.XAF.ModelEditor\Xpand.XAF.ModelEditor.csproj" --source $packageSources --source (Get-PackageFeed -Nuget) /WarnAsError
        dotnet msbuild "$Root\tools\Xpand.XAF.ModelEditor\Xpand.XAF.ModelEditor.csproj" "/bl:$Root\Bin\ModelEditor.binlog" "/p:configuration=Release" /WarnAsError /m /v:m
    } -Maximum 3
    
    Write-HostFormatted "Build Versions:" -Section
    Get-ChildItem "$Root\Bin" "*Xpand.*.dll"|ForEach-Object{
        [PSCustomObject]@{
            Id = $_.BaseName
            Version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion
        }|Write-Output
    }|Format-Table
    Get-ChildItem $root\bin "*xpand*.dll"| Test-AssemblyReference -VersionFilter $DXVersion|Format-Table -AutoSize
}

Task  CreateNuspec {
    Invoke-Script {
        $a = @{
            DxVersion = $dxVersion
            Branch=$branch
        }
        New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force | Out-Null
        $version=(& (Get-NugetPath) list -source "$root\bin\nupkg;"|ConvertTo-PackageObject|Select-Object -First 1).Version
        $currentVersion=[System.Diagnostics.FileVersionInfo]::GetVersionInfo("$root\bin\Xpand.XAF.Modules.Reactive.dll").FileVersion
        if ($currentVersion -ne $version){
            Get-ChildItem "$root\bin\nupkg" |Remove-Item -Force
        }
        
        if (!(Test-Path "$Root\bin\Nupkg") -or !(Get-ChildItem "$Root\bin\Nupkg")){
            & "$PSScriptRoot\CreateNuspec.ps1" @a
        }
        
    } -Maximum 3
}

Task PackNuspec {
    Invoke-Script {
        if (!(Test-Path "$Root\bin\Nupkg") -or !(Get-ChildItem "$Root\bin\Nupkg")){
            & "$PSScriptRoot\PackNuspec.ps1" -branch $branch -dxversion $dxVersion
        }
    } -Maximum 3
}


Task Clean -precondition { return $cleanBin } {
    $bin = "$Root\bin\"
    if (Test-Path $bin) {
        Get-ChildItem $bin -Recurse -Exclude "*Nupkg*"|Remove-Item -Force -Recurse
    }
    Set-Location "$Root\src"
    Clear-XProjectDirectories
}

Task CheckVersions -precondition { return $branch -eq "master" } {
    Push-Location "$Root\bin\"
    $labPackages=Get-ChildItem Xpand*.dll|Where-Object{([version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($_).FileVersion).Revision -gt 0}
    if ($labPackages){
        $labPackages
        throw "Lab packages found in a release build"
    }    
    Pop-Location   
}

Task ? -Description "Helper to display task info" {
    Write-Documentation
}
