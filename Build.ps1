Framework "4.6"
$ErrorActionPreference = "Stop"


properties {
    $packageSources=$null
    $nugetBin = "$PSScriptRoot\bin\nuget\"
    $msbuild = $null
    $cleanBin = $null
    $nugetApiKey = $null
    $compile = $true
    $dxVersion=$null
    $branch=$null
    $Release=$null
}

task Release  -depends  DiscoverMSBuild, Clean, Init, UpdateProjects, RestoreNuggets, Compile,IndexSources, CreateNuspec, PackNuspec, UpdateReadMe


Task IndexSources{
    InvokeScript{
        Get-ChildItem "$PSScriptRoot\bin" Xpand.XAF.Modules.*.pdb| Update-XSymbols -SourcesRoot "$PSScriptRoot" -TargetRoot "https://raw.githubusercontent.com/eXpandFramework/DevExpress.XAF/$branch"
    }
}

task Init {
    InvokeScript{
        New-Item "$PSScriptRoot\bin" -ItemType Directory -Force |Out-Null
        Install-XDevExpress -dxSource $packageSources -binPath "$PSScriptRoot\bin" -sourcePath "$PSScriptRoot\src" -dxVersion $dxVersion
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

task RestoreNuggets {
    InvokeScript{
        # Get-ChildItem *.sln -Recurse|ForEach-Object{
        #     Push-Location $_.DirectoryName
        #     if ($packageSources){
        #         $sources= "https://api.nuget.org/v3/index.json;$packageSources"
        #         & dotnet restore -s $sources
        #     }
        #     else {
        #         & dotnet restore
        #     }
        #     Pop-Location
        # }
    }
}

task Compile -precondition {return $compile  } {
    $source="https://api.nuget.org/v3/index.json;$packageSources"
    InvokeScript{
        write-host "Building Extensions" -f "Blue"
        & dotnet build "$PSScriptRoot\src\Extensions\Extensions.sln" --configuration Release --source $source
    }
    InvokeScript{
        write-host "Building Modules" -f "Blue"
        & dotnet build "$PSScriptRoot\src\Modules\Modules.sln" --configuration Release --source $source
    }
    InvokeScript{
        write-host "Building Tests" -f "Blue"
        & dotnet build "$PSScriptRoot\src\Tests\Tests.sln" --configuration Release --source $source --output $PSScriptRoot\bin
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

Task DiscoverMSBuild {
    InvokeScript {
        if (!$msbuild) {
            $script:msbuild = (Get-XMsBuildPath)
        }
        else {
            $script:msbuild = $msbuild
        }
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