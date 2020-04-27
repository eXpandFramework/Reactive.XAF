using namespace System
using namespace System.Threading
using namespace System.Reflection
using namespace System.IO
using namespace System.IO.Compression
using namespace System.Reflection
using namespace System.Text.RegularExpressions
using namespace Mono.Cecil
using namespace Mono.Cecil.pdb
param(
    [string]$SolutionPath = "C:\Work\eXpandFramework\Issues\Solution12\Solution12.sln",
    [string]$ProjectPath = "C:\Work\eXpandFramework\Issues\Solution12\Solution12.Module\Solution12.Module.csproj",
    [string]$OutputPath = "bin\debug",
    [string]$DevExpressVersion,
    [string]$VerboseOutput = "SilentlyContinue" 
)

. $PSScriptRoot\Functions.ps1
$VerbosePreference=ConfigureVerbose $VerboseOutput ModelEditorVerbose
if (!$DevExpressVersion) {
    $targetPath = [Path]::GetDirectoryName((GetAssemblypath $ProjectPath $OutputPath))
    $dxVersion = GetDevExpressVersion $targetPath "DevExpress*" $projectPath
}
else {
    $dxVersion = $DevExpressVersion
}

if (!(Test-Version $dxVersion)) {
    throw "Cannot find DevExpress Version for $ProjectPath. You have the following options:`r`n1. $howToVerbose`r`n2. If your project has indirect references to DevExpress through another assembly then you can always force the DevExpressVersion by modifying your project to include <PropertyGroup><DevExpressVersion>19.1.3</DevExpressVersion>.`r`n This declaration can be solution wide if done in your directory.build.props file.`r`n"
}
Write-VerboseLog "DxVersion=$dxVersion"
if (Test-Path "$PSScriptRoot\ModelerLibDownloader\bin\$dxVersion"){
    Write-Verbose "Dependencies found"
    CreateExternalIDETool $SolutionPath $ProjectPath
    CreateModelEditorBootStrapper $SolutionPath $dxversion $OutputPath
    return
}

try {
    $mtx = [Mutex]::OpenExisting("ModelEditorMutex")
}
catch {
    $mtx = [Mutex]::new($false, "ModelEditorMutex")
}
$mtx.WaitOne() | Out-Null
if (Test-Path "$PSScriptRoot\ModelerLibDownloader\bin\$dxVersion"){
    Write-Verbose "Dependencies found exiting"
    return
}
try {    
    DownloadDependencies $dxversion
    Install-MonoCecil $targetPath
    $a = @{
        Modulepath      = "$PSScriptRoot\ModelerLibDownloader\bin\$dxVersion\Xpand.XAF.ModelEditor.exe"
        Version         = $dxversion
        referenceFilter = "DevExpress*"
        snkFile         = "$PSScriptRoot\Xpand.snk"
        AssemblyList    = @()
    }
    $a
    Switch-AssemblyDependencyVersion @a
    CreateExternalIDETool $SolutionPath $ProjectPath
    CreateModelEditorBootStrapper $SolutionPath $dxversion $OutputPath
}
catch {
    throw "$_`r`n$($_.ScriptStackTrace)"
}
finally {
    try {
        $mtx.ReleaseMutex() | Out-Null
        $mtx.Dispose() | Out-Null
    }
    catch {
        
    }
}



