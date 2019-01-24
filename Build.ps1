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
}

task default  -depends InstallModules, DiscoverMSBuild, Clean, Init, UpdateProjects, RestoreNuggets, Compile, CreateNuspec, PackNuspec, PublishNuget

task InstallModules{
    & "$PSScriptRoot\Tools\Build\Install-Module.ps1" $([PSCustomObject]@{
        Name = "XpandPosh"
        Version ="1.0.5"
    })
}

task Init {
    InvokeScript{
        New-Item "$PSScriptRoot\bin" -ItemType Directory -Force |Out-Null
        Install-XNugetCommandLine
        Install-XDX -dxSource $packageSources -binPath "$PSScriptRoot\bin" -sourcePath "$PSScriptRoot\src" -dxVersion $dxVersion
    }
}

task UpdateProjects {
    InvokeScript{
        & "$PSScriptRoot\tools\build\UpdateProjects.ps1"
    }
}

task RestoreNuggets {
    InvokeScript{
        Get-ChildItem *.sln -Recurse|ForEach-Object{
            Push-Location $_.DirectoryName
            if ($packageSources){
                $sources= "https://api.nuget.org/v3/index.json;$packageSources"
                & nuget restore -source $sources
            }
            else {
                & nuget restore
            }
            Pop-Location
        }
    }
}

task Compile -precondition {return $compile  } {
    InvokeScript{
        write-host "Building Extensions" -f "Blue"
        & $script:msbuild "$PSScriptRoot\src\Extensions\Extensions.sln" /p:Configuration=Release /fl /v:m
    }
    InvokeScript{
        write-host "Building Modules" -f "Blue"
        & $script:msbuild "$PSScriptRoot\src\Modules\Modules.sln" /p:Configuration=Release /fl /v:m
    }
    InvokeScript{
        write-host "Building Specifications" -f "Blue"
        & $script:msbuild "$PSScriptRoot\src\Specifications\Specifications.sln" "/p:Configuration=Release;OutputPath=$PSScriptRoot\bin" /fl /v:m
    }
    
    # Get-ChildItem *.csproj -Recurse|ForEach-Object{
    #     [xml]$csProj=Get-Content $_.FullName
    #     $csProj.Project.ItemGroup.Reference|Where{$_.Include -like "DevExpress*"}|ForEach-Object{
    #         $_.ChildNodes|Where{$_.Name -eq "HintPath"}|ForEach-Object{
    #             $_.ParentNode.RemoveChild($_)|out-null
    #         }
    #     }
    #     $csProj.Save($_.FullName)
    # }
}

Task PublishNuget -precondition {return $nugetApiKey} {
    InvokeScript{
        Get-ChildItem -Path $nugetBin -Filter *.nupkg|ForEach-Object {
            & nuget push $_.FullName $nugetApiKey -source https://api.nuget.org/v3/index.json
        }
    }
}

Task  CreateNuspec  {
    InvokeScript{
        & "$PSScriptRoot\tools\build\CreateNuspec.ps1"
    }
}

Task PackNuspec {
    InvokeScript {
        & .\tools\build\PackNuspec.ps1 
    }
}

Task DiscoverMSBuild {
    InvokeScript {
        if (!$msbuild) {
            $script:msbuild = (Get-XMsBuildLocation)
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
        Write-Warning $_.Exception
        exit 1
    }
}