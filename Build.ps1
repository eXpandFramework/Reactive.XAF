Framework "4.6"
$ErrorActionPreference = "Stop"
. .\tools\Invoke-InParallel.ps1

properties {
    $packageSources=$null
    $nugetExe = "$PSScriptRoot\tools\Nuget.exe"
    $nugetBin = "$PSScriptRoot\bin\nuget\"
    $version = $null
    $nuspecFiles = $null
    $filter = $null
    $msbuild = $null
    $cleanBin = $null
    $nugetApiKey = $null
    $build = $true
}

task default  -depends DiscoverMSBuild, Clean,ChangeAssemblyInfo,Init , UpdateProjects,RestoreNuggets,Compile ,CreateNuspec,PackNuspec,PublishNuget

task Init {
    New-Item "$PSScriptRoot\bin" -ItemType Directory -Force |Out-Null
}

task ChangeAssemblyInfo {
    # Get-ChildItem "*AssemblyInfo.cs" -Recurse|ForEach-Object{
    #     $c=Get-Content $_ 
    #     $result = $c -creplace 'Version\("([^"]*)', "Version(""$version"
    #     Set-Content $_ -Value $result
    # }
}

task UpdateProjects {
    & .\tools\build\UpdateProjects.ps1
}

task RestoreNuggets {
    Exec {
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

task Compile -precondition {return $build  } {
    exec {
        write-host "Building Extensions" -f "Blue"
        & $script:msbuild "$PSScriptRoot\src\Extensions\DevExpress.XAF.Extensions.sln" /p:Configuration=Release /fl /v:m
        write-host "Building Modules" -f "Blue"
        & $script:msbuild "$PSScriptRoot\src\Modules\DevExpress.XAF.Modules.sln" /p:Configuration=Release /fl /v:m
        write-host "Building Specifications" -f "Blue"
        & $script:msbuild "$PSScriptRoot\src\Specifications\DevExpress.XAF.Agnostic.Specifications.sln" "/p:Configuration=Release;OutputPath=$PSScriptRoot\bin" /fl /v:m
    }
}

Task PublishNuget -precondition {return $nugetApiKey} {
    Exec {
        Get-ChildItem -Path $nugetBin -Filter *.nupkg|ForEach-Object {
            & $nugetExe push $_.FullName $nugetApiKey -source https://api.nuget.org/v3/index.json
        }
    }
}

Task  CreateNuspec  {
    & .\tools\build\CreateNuspec.ps1 $version
}

Task PackNuspec {
    Exec {
        New-Item $nugetBin -ItemType Directory -Force|Out-Null
        $packData = [pscustomobject] @{
            version  = $version
            nugetBin = $nugetBin
            nugetExe = $nugetExe
        }
        $assemblyVersions=Get-ChildItem *.csproj -Recurse|foreach{
            $assemblyInfo=get-content "$($_.DirectoryName)\Properties\AssemblyInfo.cs"
            [PSCustomObject]@{
                Name = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
                Version =[System.Text.RegularExpressions.Regex]::Match($assemblyInfo,'Version\("([^"]*)').Groups[1].Value
            }
        }
        $items=Get-ChildItem $("$PSScriptRoot"+"$nuspecFiles") -Include $filter -Recurse|foreach{
            $packageName=[System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
            $v=$assemblyVersions|where{$_.name -eq $packageName}|Select -First 1
            [PSCustomObject]@{
                FullName = $_.FullName
                Version = $v.Version
                Basepath = $_.Directory.Parent.FullName
            }
        }
        Invoke-InParallel -InputObject $items -Parameter $packData -runspaceTimeout 30  -ScriptBlock {              
            $name=$_.FullName
            $directory=$_.Basepath
            & $parameter.nugetExe pack $name -OutputDirectory $parameter.nugetBin -Basepath $directory -Version $_.Version
        }
    }
}

Task DiscoverMSBuild {
    Exec {
        if (!$msbuild) {
            $script:msbuild = (FindMSBuild)
        }
        else {
            $script:msbuild = $msbuild
        }
    }
}

task Clean -precondition {return $cleanBin} {
    exec {
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

