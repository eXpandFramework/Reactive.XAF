Framework "4.6"
$ErrorActionPreference = "Stop"


properties {
    $packageSources=$null
    $nugetBin = "$PSScriptRoot\bin\nuget\"
    $cleanBin = $null
    $nugetApiKey = $null
    $compile = $true
    $dxVersion=$null
    $branch=$null
    $Release=$null
    $CustomVersion=$false
    $AzureToken=$null,
    $Root="$nugetbin\..\..\"
}


task Release  -depends   Clean, Init, UpdateProjects,  Compile,IndexSources, CreateNuspec, PackNuspec, CompileTests,UpdateAllTests
task TestsRun  -depends Release

Task IndexSources{
    InvokeScript{
        $sha=Get-GitLastSha "https://github.com/eXpandFramework/DevExpress.XAF" $branch
        Get-ChildItem "$root\bin" Xpand.XAF.Modules.*.pdb| Update-XSymbols -SourcesRoot "$Root" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$sha"
    }
}

task Init {
    InvokeScript{
        "Create bin directory in $Root"
        New-Item "$Root\bin" -ItemType Directory -Force 
        New-Item "$Root\bin\ReactiveLoggerClient" -ItemType Directory -Force |Out-Null
        
        Get-ChildItem "Tests.runsettings" -Recurse|Copy-Item -Destination "$Root\bin\Tests.runsettings" -Force
        $versionMismatch=Get-ChildItem $Root *.csproj -Recurse -Exclude "*TestApplication*"|ForEach-Object{
            $projectPath=$_.FullName
            Get-PackageReference $projectPath|foreach-Object{
                if ($_.Include -like "DevExpress*" -and $_.Version -ne $dxVersion){
                    [PSCustomObject]@{
                        Path = $projectPath
                        Version=$_.Version
                    }
                }
            }
        }|Select-Object -First 1
        if ($versionMismatch -and !$CustomVersion){
            throw "$($versionMismatch.Path) use DX $($versionMismatch.Version) instaed of $dxversion"
        }
    }
}

task UpdateProjects {
    InvokeScript{
        & "$PSScriptRoot\UpdateProjects.ps1"
    }
}

task UpdateReadMe {
    InvokeScript{
        & "$PSScriptRoot\UpdateReadMe.ps1" 
    }
}

task CompileTests -precondition {return $compile  } {
    InvokeScript -maxRetries 3{
        write-host "Building Tests" -f "Blue"
        $source="https://api.nuget.org/v3/index.json;$packageSources"
        $source="$source;$Root\Bin\Nupkg"
        dotnet restore "$Root\src\Tests\Tests.sln" --source $packageSources --source (Get-PackageFeed -Nuget) --source  "$Root\Bin\Nupkg" /WarnAsError
        dotnet msbuild "$Root\src\Tests\Tests.sln" "/bl:$Root\Bin\CompileTests.binlog" "/p:configuration=Debug" /WarnAsError /m /v:m
    }
}

task Compile -precondition {return $compile  } {
    InvokeScript -maxRetries 3 {
        write-host "Building Extensions" -f "Blue"
        dotnet restore "$Root\src\Extensions\Extensions.sln" --source (Get-PackageFeed -nuget) --source $packageSources /WarnAsError
        & dotnet msbuild "$Root\src\Extensions\Extensions.sln" "/bl:$Root\Bin\Extensions.binlog" "/p:configuration=Release" /m /v:m
    }
    InvokeScript -maxRetries 3{
        write-host "Building Modules" -f "Blue"
        dotnet restore "$Root\src\Modules\Modules.sln" --source (Get-PackageFeed -nuget) --source $packageSources /WarnAsError
        set-location "$Root\src\Modules"
        dotnet msbuild "$Root\src\Modules\Modules.sln" "/bl:$Root\Bin\Modules.binlog" "/p:configuration=Release" /WarnAsError /m /v:m
    }
}

Task  CreateNuspec  {
    InvokeScript{
        $a=@{
            Release=$Release
            DxVersion=$dxVersion
        }
        & "$PSScriptRoot\CreateNuspec.ps1" @a
    }
}

Task PackNuspec {
    InvokeScript {
        & "$PSScriptRoot\PackNuspec.ps1" -branch $branch
    }
}

Task UpdateAllTests {
    InvokeScript -maxRetries 3 {
        & "$Root\Tools\Build\UpdateAllTests.ps1" "$Root" $branch $packageSources $dxVersion
    }
}


task Clean -precondition {return $cleanBin} {
    InvokeScript {
        $bin="$Root\bin\"
        Remove-Item $bin -Recurse -Force -ErrorAction SilentlyContinue
        Clear-XProjectDirectories
    }
}

task ? -Description "Helper to display task info" {
    Write-Documentation
}

function InvokeScript($sb,$maxRetries=0){
    try {
        exec $sb -maxRetries $maxRetries
    }
    catch {
        Write-Error ($_.Exception | Format-List -Force | Out-String) -ErrorAction Continue
        Write-Error ($_.InvocationInfo | Format-List -Force | Out-String) -ErrorAction Continue
        exit 1
    }
}