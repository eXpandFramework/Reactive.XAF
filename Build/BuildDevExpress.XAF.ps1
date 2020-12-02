Framework "4.6"
$ErrorActionPreference = "Stop"


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



Task Build  -depends   Clean, Init, UpdateProjects, Compile,CheckVersions, IndexSources, CreateNuspec, PackNuspec, CompileTests
Task TestsRun  -depends   Clean, Init, UpdateProjects, Compile,CheckVersions, IndexSources, CreateNuspec, PackNuspec, CompileTests


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
        & "$Root\Build\UpdateAllTests.ps1" "$Root" $branch $packageSources $dxVersion
    }
    Get-ChildItem $root\bin "*xpand*.dll"| Test-AssemblyReference -VersionFilter $DXVersion
}

Task Compile -precondition { return $compile } {
    $Configuration="Debug"
    if ($branch -eq "master"){
        $Configuration="Release"
    }
    
    Invoke-Script {
        Write-HostFormatted "Building Extensions" -Section
        & dotnet msbuild "$Root\src\Extensions\Extensions.sln" /t:Clean
        dotnet restore "$Root\src\Extensions\Extensions.sln" --source $packageSources --source (Get-PackageFeed -Nuget) /WarnAsError
        & dotnet msbuild "$Root\src\Extensions\Extensions.sln" "/bl:$Root\Bin\Extensions.binlog" "/p:configuration=$Configuration;skipNugetReplace=true" /m /v:m /WarnAsError 
    } -Maximum 2
    
    
    Invoke-Script {
        Write-HostFormatted "Building Modules" -Section
        dotnet msbuild "$Root\src\Modules\Modules.sln" /t:Clean
        Set-Location "$Root\src\Modules"
        dotnet restore "$Root\src\Modules\Modules.sln" --source $packageSources --source (Get-PackageFeed -Nuget) /WarnAsError
        dotnet msbuild "$Root\src\Modules\Modules.sln" "/bl:$Root\Bin\Modules.binlog" "/p:configuration=$Configuration;skipNugetReplace=true" /WarnAsError /m /v:m
    } -Maximum 2
    
    Invoke-Script {
        Write-HostFormatted "Building Xpand.XAF.ModelEditor" -Section
        dotnet msbuild "$Root\tools\Xpand.XAF.ModelEditor\Xpand.XAF.ModelEditor.csproj" /t:Clean
        dotnet restore "$Root\tools\Xpand.XAF.ModelEditor\Xpand.XAF.ModelEditor.csproj" --source $packageSources --source (Get-PackageFeed -Nuget) /WarnAsError
        dotnet msbuild "$Root\tools\Xpand.XAF.ModelEditor\Xpand.XAF.ModelEditor.csproj" "/bl:$Root\Bin\ModelEditor.binlog" "/p:configuration=$Configuration" /WarnAsError /m /v:m
    } -Maximum 2
    
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
        & "$PSScriptRoot\CreateNuspec.ps1" @a
    } -Maximum 3
}

Task PackNuspec {
    Invoke-Script {
        & "$PSScriptRoot\PackNuspec.ps1" -branch $branch 
    } -Maximum 3
}


Task Clean -precondition { return $cleanBin } {
    $bin = "$Root\bin\"
    if (Test-Path $bin) {
        Get-ChildItem $bin -Recurse|Remove-Item -Force -Recurse
    }
        
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
