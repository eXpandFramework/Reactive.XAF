function Update-AssemblyInfoBuild($path){
    if (!$path){
        $path= "."
    }
    Get-ChildItem -path $path -filter "*AssemblyInfo.cs" -Recurse|ForEach-Object{
        $c=Get-Content $_.FullName
        $r=new-object System.Text.RegularExpressions.Regex("[\d]{2}\.[\d]{1}\.[\d]*(\.[\d]*)?")
        $version=New-Object System.Version ($r.Match($c).Value)
        $newBuild=$version.Build+1
        $newVersion=new-object System.Version ($version.Major,$version.Minor,$newBuild,0)
        $result = $c -creplace 'Version\("([^"]*)', "Version(""$newVersion"
        Set-Content $_.FullName $result
    }
}
function Update-AssemblyInfoVersion([parameter(mandatory)]$version,$path){
    if ($path -eq $null){
        $path= "."
    }
    Get-ChildItem -path $path -filter "*AssemblyInfo.cs" -Recurse|ForEach-Object{
        $c=Get-Content $_.FullName
        $result = $c -creplace 'Version\("([^"]*)', "Version(""$version"
        Set-Content $_.FullName $result
    }
}

function Get-XpandVersion ($XpandPath) { 
    $assemblyInfo="$XpandPath\Xpand\Xpand.Utils\Properties\XpandAssemblyInfo.cs"
    $matches = Get-Content $assemblyInfo -ErrorAction Stop | Select-String 'public const string Version = \"([^\"]*)'
    if ($matches) {
        return $matches[0].Matches.Groups[1].Value
    }
    else{
        Write-Error "Version info not found in $assemblyInfo"
    }
}

function Show-Colors {
    $Colors = [Enum]::GetValues([ConsoleColor])
    $Max = ($Colors | ForEach-Object { "$_".Length } | Measure-Object -Maximum).Maximum
    foreach ($Color in $Colors) {
        Write-Host ("{0, 2} {1, $Max} " -f [int]$Color, $Color) -NoNewline
        Write-Host $Color -Foreground $Color
    }
}

function Get-RelativePath($fileName,$targetPath) {
    $location=Get-Location
    Set-Location $((get-item $filename).DirectoryName)
    $path=Resolve-Path $targetPath -Relative
    Set-Location $location
    return $path
}

function Get-DXVersion($version,$build){
    $v=New-Object System.Version $version
    if (!$build){
        "$($v.Major).$($v.Minor)"
    }    
    else{
        "$($v.Major).$($v.Minor).$($v.Build.ToString().Substring(0,1))"
    }
}
function Write-HostHashtable($params){
    foreach ($key in $params.keys) {
        write-host "$key=$($params[$key])" -f "Blue"
    }
}
function Get-AllParameters([System.Management.Automation.InvocationInfo]$invocation,$variables){
    $params=@{}
    foreach($key in $invocation.MyCommand.Parameters.Keys) {
        if (((!$params.ContainsKey($key)))) {
            $variables| ForEach-Object{
                if ($_.Name -eq $key){
                    $params[$key] = $_.Value
                }
            }            
        }
    }
    return $params
}

function Get-DXPath($version){
    $v=Get-DXVersion $version
    try{
        $i=Get-Item "HKLM:\SOFTWARE\WOW6432Node\DevExpress\Components\v$v\"
        $i.GetValue("RootDirectory")
    }
    catch{}
}

function Get-MsBuildLocation{
    if (!(Get-Module -ListAvailable -Name VSSetup)) {
        Set-PSRepository -Name "PSGallery" -InstallationPolicy Trusted
        Install-Module VSSetup
    }
    $msbuildPath=Get-VSSetupInstance  | Select-VSSetupInstance -Product Microsoft.VisualStudio.Product.BuildTools -Latest |Select-Object -ExpandProperty InstallationPath
    if (!$msbuildPath){
        throw "VS 2017 build tools not found. Please download from https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2017"
    }
    
    if (!(Get-VSSetupInstance  | Select-VSSetupInstance -Product Microsoft.VisualStudio.Product.BuildTools -Require Microsoft.VisualStudio.Component.NuGet.BuildTools -Latest|Select-Object -ExcludeProperty InstallationPath)){
        throw "MsBuild Nuget targets missing. https://stackoverflow.com/questions/47797510/the-getreferencenearesttargetframeworktask-task-was-not-found"
    }
    if (!(Get-VSSetupInstance  | Select-VSSetupInstance -Product Microsoft.VisualStudio.Product.BuildTools -Require Microsoft.VisualStudio.Workload.WebBuildTools -Latest|Select-Object -ExcludeProperty InstallationPath)){
        throw "MsBuild WebBuildTools missing. https://stackoverflow.com/questions/44061932/ms-build-2017-microsoft-webapplication-targets-is-missing"
    }
    join-Path $msbuildPath MSBuild\15.0\Bin\MSBuild.exe
}

function Disable-ExecutionPolicy {
    ($ctx = $executioncontext.gettype().getfield(
        "_context","nonpublic,instance").getvalue(
            $executioncontext)).gettype().getfield(
                "_authorizationManager","nonpublic,instance").setvalue(
        $ctx, (new-object System.Management.Automation.AuthorizationManager `
                  "Microsoft.PowerShell"))
}
function Get-VersionFromFile([parameter(mandatory)][string]$assemblyInfo){
    $matches = Get-Content $assemblyInfo -ErrorAction Stop | Select-String 'public const string Version = \"([^\"]*)'
    if ($matches) {
        $matches[0].Matches.Groups[1].Value
    }
    else{
        throw "Version info not found in $assemblyInfo"
    }
}
function Start-Build($msbuild,$buildArgs){
    & $msbuild $buildArgs;
    if ($LASTEXITCODE){
        throw "Build failed $buildArgs"
    }   
}
function New-Command{
    param($commandTitle, $commandPath, $commandArguments,$workingDir)
    Try {
        $pinfo = New-Object System.Diagnostics.ProcessStartInfo
        $pinfo.FileName = $commandPath
        $pinfo.RedirectStandardError = $true
        if ($workingDir){
            $pinfo.WorkingDirectory=$workingDir
        }
        $pinfo.RedirectStandardOutput = $true
        $pinfo.UseShellExecute = $false
        $pinfo.Arguments = $commandArguments
        $p = New-Object System.Diagnostics.Process
        $p.StartInfo = $pinfo
        $p.Start() | Out-Null
        [pscustomobject]@{
            commandTitle = $commandTitle
            stdout = $p.StandardOutput.ReadToEnd()
            stderr = $p.StandardError.ReadToEnd()
            ExitCode = $p.ExitCode
        }
        $p.WaitForExit()
    }
    Catch {
        [pscustomobject]@{
            commandTitle = $commandTitle
            stderr = $_.Exception.Message
            ExitCode = 1
        }
    }
}

function Install-Chocolatey {
    Invoke-Expression ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))
}
function Restore-ProjectPackages{
    $nuget="$PSScriptRoot\..\nuget.exe"
    $project= get-project
    "Resstoring packages in $($project.FullName)"
    & $nuget restore $project.FullName
}
function Install-Nuget() {
    Install-Chocolatey
    cinst NuGet.CommandLine
}

function Clear-ASPNETTemp() {
    Get-ChildItem "C:\Windows\Microsoft.NET\Framework*\v*\Temporary ASP.NET Files" -Recurse | Remove-Item -Recurse
}

function Restart-XpandPsUtils() {
    Import-Module XpandPsUtils -Force
}

function Remove-Nuget($id) {
    Get-ChildItem -Recurse -Filter '*.csproj' | ForEach-Object { $_.FullName } | False-XpandSpecificVersions
    CleanProjectCore
    Get-ChildItem -Recurse -Filter 'packages.config' | ForEach-Object { $_.FullName } | Write-XmlComment -xPath "//package[contains(@id,'$id')]"

    Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer } | Where-Object { $_.Name -eq 'packages' } | ForEach-Object {
  		    Push-Location $_
  		    Get-ChildItem -Recurse | Where-Object { $_.PSIsContainer } |  Where-Object { $_.Name -like "$id*" } | Remove-Item -Recurse
    }        
}

function Clear-ProjectDirectories() {
    CleanProjectCore
    Get-ChildItem -Include "*.log", "*.bak" -Recurse | Remove-Item -Force
    $folders = "packages"
    Get-ChildItem -Recurse | 
        Where-Object { $_.PSIsContainer} | 
        Where-Object { $folders -contains $_.Name   } | 
        Remove-Item -Force -Recurse
}

function CleanProjectCore() {
    Get-ChildItem -Recurse | 
        Where-Object { $_.PSIsContainer } | 
        Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'obj' -or $_.Name -eq '.vs' -or $_.Name.StartsWith('_ReSharper')} | foreach{
            (Get-Item $_.FullName).Delete($true)
        }
        
        
}

function Update-PackageInProjects($projectWildCard, $packageId, $version) {
    Get-Project $projectWildCard | ForEach-Object {Update-Package  -Id $packageId -ProjectName $_.ProjectName -Version $version}
}

function Install-AllProjects($packageName) {
    Get-Project -All | Install-Package $packageName
}

function Uninstall-ProjectAllPackages($packageFilter) {
    
    while((Get-Project | Get-Package | where  {
        $_.id.Contains($packageFilter)
    } ).Length -gt 0) { 
        Get-Project | Get-Package | where  {
            $_.id.Contains($packageFilter)
        } | Uninstall-Package 
    }
    
}
function Uninstall-ProjectXAFPackages {
    Uninstall-ProjectAllPackages DevExpress.XAF.
}

Function Start-UnZip {
    [cmdletbinding()]
    Param (
        [parameter(ValueFromPipeline = $True, mandatory = $True)]
        [string]$fileName,
        [string]$dir
    )
    Begin {
        Write-Verbose "Initialize stuff in Begin block"
    }

    Process {
        Add-Type -Assembly System.IO.Compression.FileSystem
        $targetDir = $dir
        if ($dir -eq "") {
            $targetDir = Split-Path $fileName
        }
        Write-Verbose "Unzipping $fileName into $targetDir"
        [System.IO.Compression.ZipFile]::ExtractToDirectory($fileName, $targetDir)
    }

    End {
        Write-Verbose "Final work in End block"
    }
}

Function Start-Zip {
    [cmdletbinding()]
    Param (
        [parameter(ValueFromPipeline = $True, mandatory = $True)]
        [string]$fileName,
        [string]$dir,
        [System.IO.Compression.CompressionLevel]$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal,
        [bool]$deleteAfterArchiving
    )
    Begin {
        Write-Verbose "Initialize stuff in Begin block"
    }

    Process {
        Add-Type -Assembly System.IO.Compression.FileSystem
        $tempDir = [System.IO.Path]::GetTempPath()
        $targetDir = $dir
        if ($dir -eq "") {
            $targetDir = Split-Path $fileName
        }
        $fileNameWithoutPath = [io.path]::GetFileName($fileName)
        $tempFileName = [io.path]::Combine($tempDir, $fileNameWithoutPath)
        Write-Verbose "Zipping $fileName into $tempDir"
        [System.IO.Compression.ZipFile]::CreateFromDirectory($targetDir, $tempFilename, $compressionLevel, $false)
        Copy-Item -Force -Path $tempFileName -Destination $fileName
        Remove-Item -Force -Path $tempFileName
        if ($deleteAfterArchiving) {
            Get-ChildItem -Path $targetDir -Exclude $fileNameWithoutPath -Recurse | Select-Object -ExpandProperty FullName | Remove-Item -Force -Recurse
        }
    }

    End {
        Write-Verbose "Final work in End block"
    }
}

function Start-ZipProject() {
    Get-ChildItem -Recurse | 
        Where-Object { $_.PSIsContainer } | 
        Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'packages' -or $_.Name -eq 'obj' -or $_.Name -eq '.vs' -or $_.Name.StartsWith('_ReSharper')} | 
        Remove-Item -Force -Recurse 
    
    $zipFileName = [System.IO.Path]::Combine($currentLocation, $zipFileName)
    Start-Zip -fileName $zipFileName 
}

Function Write-XmlComment {

    [cmdletbinding()]
  
    Param (
        [parameter(ValueFromPipeline = $True, mandatory = $True)]
        [string]$fiLeName,
        [parameter(mandatory = $True)]
        [string]$xPath
  
    )
  
    Process {
        $xml = [xml](Get-Content $fiLeName)
        $xml.SelectNodes($xPath)| ForEach-Object { 
            $abc = $_;
            $comment = $xml.CreateComment($abc.OuterXml);
            $abc.ParentNode.ReplaceChild($comment, $abc);
        }
        $xml.Save($fiLeName)
    }
  
}

Export-ModuleMember -function Clear-ASPNETTemp
Export-ModuleMember -function Clear-ProjectDirectories
Export-ModuleMember -function Install-Chocolatey
Export-ModuleMember -function Install-AllProjects
Export-ModuleMember -function Install-Nuget
Export-ModuleMember -function Remove-Nuget
Export-ModuleMember -function Restart-XpandPsUtils
Export-ModuleMember -function Start-UnZip
Export-ModuleMember -function Start-Zip
Export-ModuleMember -function Uninstall-ProjectAllPackages
Export-ModuleMember -function Uninstall-ProjectXAFPackages
Export-ModuleMember -function Update-PackageInProjects
Export-ModuleMember -function Restore-ProjectPackages
Export-ModuleMember -function New-Command
Export-ModuleMember -function Start-Build
Export-ModuleMember -function Get-VersionFromFile
Export-ModuleMember -function Disable-ExecutionPolicy
Export-ModuleMember -function Get-MsBuildLocation
Export-ModuleMember -function Get-AllParameters
Export-ModuleMember -function Get-ScriptVariables
Export-ModuleMember -function Write-HostHashtable
Export-ModuleMember -function Get-DXPath
Export-ModuleMember -function Get-DXVersion
Export-ModuleMember -function Get-RelativePath
Export-ModuleMember -function Show-Colors
Export-ModuleMember -function Get-XpandVersion
Export-ModuleMember -function Update-AssemblyInfoVersion
Export-ModuleMember -function Update-AssemblyInfoBuild
