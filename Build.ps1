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
}

task Release  -depends   Clean, Init, UpdateProjects,  Compile,IndexSources, CreateNuspec, PackNuspec, UpdateReadMe


Task IndexSources{
    InvokeScript{
        $sha=Get-GitLastSha "https://github.com/eXpandFramework/DevExpress.XAF" $branch
        Get-ChildItem "$PSScriptRoot\bin" Xpand.XAF.Modules.*.pdb| Update-XSymbols -SourcesRoot "$PSScriptRoot" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$sha"
    }
}

task Init {
    InvokeScript{
        New-Item "$PSScriptRoot\bin" -ItemType Directory -Force |Out-Null
        $versionMismatch=Get-ChildItem $PSScriptRoot *.csproj -Recurse|ForEach-Object{
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
        if ($versionMismatch){
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

task Compile -precondition {return $compile  } {
    $source="https://api.nuget.org/v3/index.json;$packageSources"
    InvokeScript{
        write-host "Building Extensions" -f "Blue"
        dotnet restore "$PSScriptRoot\src\Extensions\Extensions.sln" --source $source
        dotnet msbuild "$PSScriptRoot\src\Extensions\Extensions.sln" "/p:configuration=Release" /WarnAsError
    }
    InvokeScript{
        write-host "Building Modules" -f "Blue"
        dotnet restore "$PSScriptRoot\src\Modules\Modules.sln" --source $source
        dotnet msbuild "$PSScriptRoot\src\Modules\Modules.sln" "/p:configuration=Release" /WarnAsError
    }
    InvokeScript{
        write-host "Building Tests" -f "Blue"
        dotnet restore "$PSScriptRoot\src\Tests\Tests.sln" --source $source
        dotnet msbuild "$PSScriptRoot\src\Tests\Tests.sln" "/p:OutDir=$PSScriptRoot\bin" 
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
        & .\tools\build\PackNuspec.ps1 -branch $branch
    }
}


task Clean -precondition {return $cleanBin} {
    InvokeScript {
        $bin="$PSScriptRoot\bin\"
        if (Test-Path $bin) {
            Get-ChildItem $bin | Remove-Item -Force -Recurse
        }
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