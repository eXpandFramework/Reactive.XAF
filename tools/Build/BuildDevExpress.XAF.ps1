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
    $Release = $null
    $CustomVersion = $false
    $AzureToken = $null,
    $Root = "$nugetbin\..\..\"
}



Task Build  -depends   Clean, Init, UpdateProjects, Compile, IndexSources, CreateNuspec, PackNuspec, CompileTests, UpdateAllTests


Task IndexSources {
    Invoke-Script {
        $sha = Get-GitLastSha "https://github.com/eXpandFramework/DevExpress.XAF" $branch
        Get-ChildItem "$root\bin" Xpand.XAF.Modules.*.pdb | Update-XSymbols -SourcesRoot "$Root" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$sha"
    }
}

Task Init {
    Invoke-Script {
        "Create bin directory in $Root"
        New-Item "$Root\bin" -ItemType Directory -Force 
        New-Item "$Root\bin\ReactiveLoggerClient" -ItemType Directory -Force | Out-Null
        
        Copy-Item -Path "$root\tools\build\Tests.runsettings" -Destination "$Root\bin\Tests.runsettings" -Force

        dotnet tool restore
        Set-Location $root
        Invoke-PaketRestore -strict
        
        Get-ChildItem "$root\packages\grpc.core\runtimes\"|Copy-Item -Destination "$root\bin\runtimes" -Verbose -Recurse -Force
        # $versionMismatch=Get-ChildItem $Root *.csproj -Recurse -Exclude "*TestApplication*"|ForEach-Object{
        #     $projectPath=$_.FullName
        #     Get-PackageReference $projectPath|foreach-Object{
        #         if ($_.Include -like "DevExpress*" -and $_.Version -ne $dxVersion){
        #             [PSCustomObject]@{
        #                 Path = $projectPath
        #                 Version=$_.Version
        #             }
        #         }
        #     }
        # }|Select-Object -First 1
        # if ($versionMismatch -and !$CustomVersion){
        #     throw "$($versionMismatch.Path) use DX $($versionMismatch.Version) instaed of $dxversion"
        # }
    }
}

Task UpdateProjects {
    Invoke-Script {
        & "$PSScriptRoot\UpdateProjects.ps1"
    }
}

Task UpdateReadMe {
    Invoke-Script {
        & "$PSScriptRoot\UpdateReadMe.ps1" 
    }
}

Task CompileTests -precondition { return $compile } {
    Invoke-Script {
        Write-Host "Building Tests" -f "Blue"
        $source = "https://api.nuget.org/v3/index.json;$packageSources"
        $source = "$source;$Root\Bin\Nupkg"
        dotnet restore "$Root\src\Tests\Tests.sln" --source $packageSources --source (Get-PackageFeed -Nuget) /WarnAsError
        dotnet msbuild "$Root\src\Tests\Tests.sln" "/bl:$Root\Bin\CompileTests.binlog" -t:rebuild "/p:configuration=Debug" /WarnAsError /m /v:m 
    } -Maximum 2
}

Task Compile -precondition { return $compile } {
    $Configuration="Debug"
    if ($release){
        $Configuration="Release"
    }
    Invoke-Script {
        Write-Host "Building Extensions" -f "Blue"
        # dotnet restore "$Root\src\Extensions\Extensions.sln" --source (Get-PackageFeed -nuget) --source $packageSources /WarnAsError
        & dotnet msbuild "$Root\src\Extensions\Extensions.sln" -t:rebuild "/bl:$Root\Bin\Extensions.binlog" "/p:configuration=$Configuration" /m /v:m /WarnAsError -r
    } -Maximum 2
    Invoke-Script {
        Write-Host "Building Modules" -f "Blue"
        # dotnet restore "$Root\src\Modules\Modules.sln" --source (Get-PackageFeed -nuget) --source $packageSources /WarnAsError
        Set-Location "$Root\src\Modules"
        dotnet msbuild "$Root\src\Modules\Modules.sln" -t:rebuild "/bl:$Root\Bin\Modules.binlog" "/p:configuration=$Configuration" /WarnAsError /m /v:m -r
    } -Maximum 2
    "Build Versions:"
    Get-ChildItem "$Root\Bin" "*Xpand.*.dll"|ForEach-Object{
        [PSCustomObject]@{
            Name = $_.BaseName
            Version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo($_.FullName)
        }
    }
}

Task  CreateNuspec {
    Invoke-Script {
        $a = @{
            Release   = $Release
            DxVersion = $dxVersion
        }
        & "$PSScriptRoot\CreateNuspec.ps1" @a
    }
}

Task PackNuspec {
    Invoke-Script {
        & "$PSScriptRoot\PackNuspec.ps1" -branch $branch
    }
}

Task UpdateAllTests {
    Invoke-Script {
        & "$Root\Tools\Build\UpdateAllTests.ps1" "$Root" $branch $packageSources $dxVersion
    }
}


Task Clean -precondition { return $cleanBin } {
    $bin = "$Root\bin\"
    if (Test-Path $bin) {
        Remove-Item $bin -Recurse -Force
    }
        
    Clear-XProjectDirectories
}

Task ? -Description "Helper to display task info" {
    Write-Documentation
}
