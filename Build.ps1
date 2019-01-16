Framework "4.6"
$ErrorActionPreference = "Stop"
. .\tools\Invoke-InParallel.ps1

properties {
    $packageSources=$null
    $nugetExe = "$PSScriptRoot\tools\Nuget.exe"
    $nugetBin = "$PSScriptRoot\bin\nuget\"
    $version = $null
    $msbuild = $null
    $cleanBin = $null
    $nugetApiKey = $null
    $compile = $true
}

task default  -depends DiscoverMSBuild, Clean, Init, UpdateProjects, RestoreNuggets, Compile, CreateNuspec, PackNuspec, PublishNuget

task Init {
    InvokeScript{
        New-Item "$PSScriptRoot\bin" -ItemType Directory -Force |Out-Null
        & .\tools\build\InstallDX.ps1 -dxSource $packageSources
    }
}

task UpdateProjects {
    InvokeScript{
        & .\tools\build\UpdateProjects.ps1
    }
}

task RestoreNuggets {
    InvokeScript{
        Get-ChildItem *.sln -Recurse|ForEach-Object{
            Push-Location $_.DirectoryName
            if ($packageSources){
                $sources= "https://api.nuget.org/v3/index.json;$packageSources"
                & $nugetExe restore -source $sources
            }
            else {
                & $nugetExe restore
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
            & $nugetExe push $_.FullName $nugetApiKey -source https://api.nuget.org/v3/index.json
        }
    }
}

Task  CreateNuspec  {
    InvokeScript{
        & .\tools\build\CreateNuspec.ps1 $version
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
            $script:msbuild = (FindMSBuild)
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
        Clear-ProjectDirectories
    }
}

function FindMSBuild() {
    if (!(Get-Module -ListAvailable -Name VSSetup)) {
        Write-Host "VSSetup Module not found"
        Install-PackageProvider -Name NuGet -Force
		Set-PSRepository -Name "PSGallery" -InstallationPolicy Trusted
        Install-Module VSSetup -Scope CurrentUser
    }
    $path = Get-VSSetupInstance  | Select-VSSetupInstance -Product Microsoft.VisualStudio.Product.BuildTools -Latest |Select-Object -ExpandProperty InstallationPath
    return join-path $path MSBuild\15.0\Bin\MSBuild.exe
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