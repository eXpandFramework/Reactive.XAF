Framework "4.6"
$ErrorActionPreference = "Stop"
$Global:Configuration = "Debug"

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

Task BuildNugetConsumers -depends   CreateNuspec, PackNuspec, CompileNugetConsumers -precondition { return ((Get-VersionPart $DXVersion Minor) -ne "19.2") } 
Task Build  -depends   Clean, Init, UpdateProjects, Compile, CheckVersions, IndexSources, CompileTests

function CompileTestSolution($solution) {
    Write-HostFormatted "Building $solution" -Section
    New-Item -Name Nupkg -ItemType Directory -Path "$root\bin" -ErrorAction SilentlyContinue
    $conf = GetConfiguration $solution "Debug"
    "Configuration=$conf"
    Start-Build -Path $solution -Configuration $conf -BinaryLogPath "$Root\Bin\Test$conf.binlog" -Verbosity minimal -WarnAsError 
    
    Copy-Item -path $root\bin\runtimes -Recurse -Destination $root\bin\TestWinApplication\runtimes -Container -Force
}

Task IndexSources {
    Invoke-Script {
        $sha = Get-GitLastSha "https://github.com/eXpandFramework/Reactive.XAF" $branch
        "Xpand.XAF.Modules.*.pdb", "Xpand.Extensions.*.pdb" | ForEach-Object {
            Get-ChildItem "$root\bin" $_ | Update-XSymbols -SourcesRoot "$Root" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/Reactive.XAF/$sha"
            # Get-ChildItem "$root\bin\net461" $_ | Update-XSymbols -SourcesRoot "$Root" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/Reactive.XAF/$sha"
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
        Invoke-Script { Invoke-PaketRestore -strict }
        
        Get-ChildItem "$(Get-NugetInstallationFolder)\grpc.core" "runtimes" -Recurse | Select-Object -Last 1 | Copy-Item -Destination "$root\bin\runtimes" -Recurse -Force
    }
}

Task UpdateProjects {
    Invoke-Script {
        & "$PSScriptRoot\UpdateProjects.ps1" $DXVersion
    }
}


Task CompileTests -precondition { return ((Get-VersionPart $DXVersion Minor) -ne "19.2") } {
    Invoke-Script {
        if ((Test-AzDevops)) {
            $nugetConfigPath = "$root\src\tests\nuget.config"
            $nugetConfig = Get-XmlContent $nugetConfigPath
            $a = @{
                key   = "DXAzDevops"
                value = $env:DxFeed
            }
            Add-XmlElement $nugetConfig "add" "packageSources" -Attributes $a
            $nugetConfig | Save-Xml $nugetConfigPath
        }
        SyncrhonizePaketVersion
        
        CompileTestSolution "$Root\src\Tests\Tests.sln"
        
        
    } -Maximum 3

    # if (!(Test-AzDevops)) {
        Invoke-Task -taskName BuildNugetConsumers
    # }
    
    Get-ChildItem $root\bin "*xpand*.dll" | Test-AssemblyReference -VersionFilter $DXVersion
    
}

function FixNet461DXAssembliesTargetFramework {
    Start-Build -Path "$root\src\Tests\ModelMapper\Xpand.XAF.Modules.ModelMapper.Tests.csproj"
}

function Update-NugetConsumersPackageVersion {
    $localVersion = Get-AssemblyInfoVersion "$root\src\Common\AssemblyInfoVersion.cs"

    Write-HostFormatted "Update Xpand package versions" -ForegroundColor Magenta
    (Get-ChildItem "$root\src\Tests\EasyTests" *.csproj -Recurse)+(Get-ChildItem "$root\tools\Xpand.XAF.ModelEditor" *.csproj -Recurse) | ForEach-Object {
        $prefs = Get-PackageReference $_ 
        $prefs | Where-Object { $_.include -like "Xpand.*" } | ForEach-Object {
            $_.version = $localVersion
        }
        ($prefs | Select-Object -First 1).OwnerDocument.Save($_)
    }
}

function SyncrhonizePaketVersion {
    Write-HostFormatted "Synchronize paket versions" -ForegroundColor Magenta
    Set-Location $root
    $pakets=Invoke-PaketShowInstalled
    (Get-MSBuildProjects "$root\src\Tests\EasyTests\")+(Get-ChildItem "$root\src\" "*Blazor*.csproj" -Recurse)|ForEach-Object{
        $_.Fullname
        [xml]$csproj=Get-XmlContent $_
        Get-PackageReference $_|Where-Object{$_.Include -notmatch "Xpand|DevExpress"} |ForEach-Object{
            $id=$_.Include
            $version=$_.Version
            $paket=$pakets|Where-Object{$_.Id -eq $id}
            if ($paket){
                if ($paket.Version -ne $version){
                    $package=$csproj.Project.ItemGroup.PackageReference|Where-Object{$_.Include -eq $id}
                    $package.version=$paket.version
                }
            }
            else{
                "$Id not found"
            }
        }
        $csproj|Save-Xml $_.Fullname
    }
}

Task CompileNugetConsumers -precondition { return $compile } {
    Invoke-Script {
        Update-NugetConsumersPackageVersion
        Start-Build "$Root\src\Tests\\EasyTests\EasyTests.sln"
        # FixNet461DXAssembliesTargetFramework
        if ($dxVersion -eq (Get-XAFLatestMinors | Select-Object -First 1)) {
            Invoke-Script {
                & $root\build\ZipMe.ps1 -SkipIDEBuild
            } -Maximum 3
        }
    } -Maximum 3
    Write-HostFormatted "Test-AssemblyReference" -Section
    Get-ChildItem $root\bin "*xpand*.dll" | Test-AssemblyReference -VersionFilter $DXVersion
}

function GetConfiguration($solution, $conf) {
    $conf = $conf.ToLower()
    $match = (Read-MSBuildSolutionFile $solution).SolutionConfigurations.ConfigurationName | Sort-Object -Unique | ForEach-Object {
        $configVersion = $_.ToLower().Replace("$conf`_", "") 
        if ((($_ -like "$conf`_*") -and (Test-Version $configVersion))) {
            if (([version]$configVersion) -ge ([version](Get-VersionPart $dxVersion Minor))) {
                $_
            }
            
        }
    }
    if ($match) {
        $match
    }
    else {
        $conf
    }
}

Task Compile -precondition { return $compile } {
    if (($branch -eq "master") -and ((Get-DevExpressVersion) -eq $DXVersion)) {
        $Global:Configuration = "Release"
    }
    
    Invoke-Script {
        Write-HostFormatted "Building Extensions" -Section
        New-Item "$Root\bin\nupkg" -ItemType Directory -Force -ErrorAction SilentlyContinue
        $solution = "$Root\src\Extensions\Extensions.sln"
        
        $Configuration = GetConfiguration $solution $Global:Configuration
        Start-Build -Path $solution -Configuration $Configuration -BinaryLogPath "$Root\Bin\Extensions.binlog" -Verbosity minimal -WarnAsError -PropertyValue "skipNugetReplace=true"
    } -Maximum 3
    
    
    Invoke-Script {
        $solution = "$Root\src\Modules\Modules.sln"
        Write-HostFormatted "Building Modules" -Section
        $Configuration = GetConfiguration $solution $Global:Configuration
        Start-Build -Path $solution -Configuration $Configuration -BinaryLogPath "$Root\Bin\Modules.binlog" -Verbosity minimal -WarnAsError -PropertyValue "skipNugetReplace=true"
    } -Maximum 3
    
    Write-HostFormatted "Build Versions:" -Section
    Get-ChildItem "$Root\Bin" "*Xpand.*.dll" | ForEach-Object {
        [PSCustomObject]@{
            Id      = $_.BaseName
            Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName).FileVersion
        } | Write-Output
    } | Format-Table
    Get-ChildItem $root\bin "*xpand*.dll" | Test-AssemblyReference -VersionFilter $DXVersion | Format-Table -AutoSize
}

Task  CreateNuspec {
    Invoke-Script {
        $a = @{
            DxVersion = $dxVersion
            Branch    = $branch
        }
        New-Item -Path "$root\bin\Nupkg" -ItemType Directory  -ErrorAction SilentlyContinue -Force | Out-Null
        $version = (Get-NugetPackageSearchMetadata -Name Xpand.XAF.Modules.Reactive -Source C:\Work\Reactive.XAF\bin\nupkg\).identity.version.version
        $currentVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("$root\bin\Xpand.XAF.Modules.Reactive.dll").FileVersion
        if ($currentVersion -ne $version) {
            Get-ChildItem "$root\bin\nupkg" | Remove-Item -Force
        }
        
        if (!(Test-Path "$Root\bin\Nupkg") -or !(Get-ChildItem "$Root\bin\Nupkg")) {
            & "$PSScriptRoot\CreateNuspec.ps1" @a
        }
        
    } -Maximum 3
}

Task PackNuspec {
    Invoke-Script {
        if (!(Test-Path "$Root\bin\Nupkg") -or !(Get-ChildItem "$Root\bin\Nupkg")) {
            & "$PSScriptRoot\PackNuspec.ps1" -branch $branch -dxversion $dxVersion
        }
    } -Maximum 3
}


Task Clean -precondition { return $cleanBin } {
    $bin = "$Root\bin\"
    if (Test-Path $bin) {
        Get-ChildItem $bin -Recurse -Exclude "*Nupkg*" | Remove-Item -Force -Recurse
    }
    Set-Location "$Root\src"
    Clear-XProjectDirectories
    Set-Location "$Root\Tools\Xpand.XAF.ModelEditor\IDE"
    Get-ChildItem . -Recurse -Include "build","output",".gradle"|Remove-Item -Force -Recurse        
}

Task CheckVersions -precondition { return $branch -eq "master" } {
    Push-Location "$Root\bin\"
    $labPackages = Get-ChildItem Xpand*.dll | Where-Object { ([version][System.Diagnostics.FileVersionInfo]::GetVersionInfo($_).FileVersion).Revision -gt 0 }
    if ($labPackages) {
        $labPackages
        throw "Lab packages found in a release build"
    }    
    Pop-Location   
}

Task ? -Description "Helper to display task info" {
    Write-Documentation
}
