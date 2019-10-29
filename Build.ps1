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
    $AzureToken=$null
}


task Release  -depends   Clean, Init, UpdateProjects,  Compile,IndexSources, CreateNuspec, PackNuspec, CompileTests,UpdateAllTests
task TestsRun  -depends Release

Task IndexSources{
    InvokeScript{
        $sha=Get-GitLastSha "https://github.com/eXpandFramework/DevExpress.XAF" $branch
        Get-ChildItem "$PSScriptRoot\bin" Xpand.XAF.Modules.*.pdb| Update-XSymbols -SourcesRoot "$PSScriptRoot" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$sha"
    }
}

task Init {
    InvokeScript{
        New-Item "$PSScriptRoot\bin\ReactiveLoggerClient" -ItemType Directory -Force |Out-Null
        
        Get-ChildItem "CodeCoverage.runsettings" -Recurse|Copy-Item -Destination "$PSScriptRoot\bin\CodeCoverage.runsettings" -Force
        $versionMismatch=Get-ChildItem $PSScriptRoot *.csproj -Recurse -Exclude "*TestApplication*"|ForEach-Object{
            $projectPath=$_.FullName
            [xml]$csproj=Get-Content $projectPath
            $csproj.project.itemgroup.packageReference|foreach-Object{
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
        & "$PSScriptRoot\tools\build\UpdateProjects.ps1"
    }
}

task UpdateReadMe {
    InvokeScript{
        & "$PSScriptRoot\tools\build\UpdateReadMe.ps1" 
    }
}

task CompileTests -precondition {return $compile  } {
    InvokeScript -maxRetries 3{
        write-host "Building Tests" -f "Blue"
        $source="https://api.nuget.org/v3/index.json;$packageSources"
        $source="$source;$PSScriptRoot\Bin\Nupkg"
        dotnet restore "$PSScriptRoot\src\Tests\Tests.sln" --source $packageSources --source (Get-PackageFeed -Nuget) --source  "$PSScriptRoot\Bin\Nupkg" /WarnAsError
        dotnet msbuild "$PSScriptRoot\src\Tests\Tests.sln" "/bl:$PSScriptRoot\Bin\CompileTests.binlog" "/p:configuration=Debug" /WarnAsError /m /v:m
    }
}

task Compile -precondition {return $compile  } {
    InvokeScript -maxRetries 3 {
        write-host "Building Extensions" -f "Blue"
        dotnet restore "$PSScriptRoot\src\Extensions\Extensions.sln" --source (Get-PackageFeed -nuget) --source $packageSources /WarnAsError
        & dotnet msbuild "$PSScriptRoot\src\Extensions\Extensions.sln" "/bl:$PSScriptRoot\Bin\Extensions.binlog" "/p:configuration=Release" /m /v:m
    }
    InvokeScript -maxRetries 3{
        write-host "Building Modules" -f "Blue"
        dotnet restore "$PSScriptRoot\src\Modules\Modules.sln" --source (Get-PackageFeed -nuget) --source $packageSources /WarnAsError
        set-location "$PSScriptRoot\src\Modules"
        dotnet msbuild "$PSScriptRoot\src\Modules\Modules.sln" "/bl:$PSScriptRoot\Bin\Modules.binlog" "/p:configuration=Release" /WarnAsError /m /v:m
    }
}

Task  CreateNuspec  {
    InvokeScript{
        $a=@{
            Release=$Release
        }
        & "$PSScriptRoot\tools\build\CreateNuspec.ps1" @a
    }
}

Task PackNuspec {
    InvokeScript {
        & "$PSScriptRoot\tools\build\PackNuspec.ps1" -branch $branch
    }
}

Task UpdateAllTests {
    InvokeScript -maxRetries 3 {
        & "$PSScriptRoot\tools\build\UpdateAllTests.ps1" $PSScriptRoot $branch $packageSources $dxVersion
    }
}


task Clean -precondition {return $cleanBin} {
    InvokeScript {
        $bin="$PSScriptRoot\bin\"
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