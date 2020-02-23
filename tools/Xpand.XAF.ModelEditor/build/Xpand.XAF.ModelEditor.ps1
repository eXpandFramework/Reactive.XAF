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
    [string]$SolutionPath = "C:\Work\eXpandFramework\Issues\Solution20\2\Solution20.sln",
    [string]$ProjectPath = "C:\Work\eXpandFramework\Issues\Solution20\2\Solution20.Module\Solution20.Module.csproj",
    [string]$OutputPath = "bin\debug",
    [string]$DevExpressVersion
)

. $PSScriptRoot\Functions.ps1

if (!$DevExpressVersion) {
    $assemblyPath = GetAssemblypath $ProjectPath $OutputPath
    $dxVersion = GetDevExpressVersion $assemblyPath "DevExpress*" $projectPath
}
else {
    $dxVersion = $DevExpressVersion
}

if (!$dxVersion) {
    throw "Cannot find DevExpress Version. If your project has indirect references to DevExpress through another assembly then you can always force the DevExpressVersion by modifying your project to include <PropertyGroup><DevExpressVersion>19.1.3</DevExpressVersion>.`r`n This declaration can be solution wide if done in your directory.build.props file.`r`n"
}
if (Test-Path "$PSScriptRoot\ModelerLibDownloader\bin\$dxVersion"){
    CreateExternalIDETool $SolutionPath $ProjectPath
    CreateModelEditorBootStrapper $SolutionPath $dxversion $OutputPath
    return
}
Write-Host "DxVersion=$dxVersion"
try {
    $mtx = [Mutex]::OpenExisting("ModelEditorMutex")
}
catch {
    $mtx = [Mutex]::new($false, "ModelEditorMutex")
}
$mtx.WaitOne() | Out-Null
if (Test-Path "$PSScriptRoot\ModelerLibDownloader\bin\$dxVersion"){
    return
}
try {    
    Install-MonoCecil $targetPath
    $a = @{
        Modulepath      = "$PSScriptRoot\Xpand.XAF.ModelEditor.exe"
        Version         = $dxversion
        referenceFilter = "DevExpress*"
        snkFile         = "$PSScriptRoot\Xpand.snk"
        AssemblyList    = @()
    }
    $a
    Switch-AssemblyDependencyVersion @a
    DownloadDependencies $dxversion
    CreateExternalIDETool $SolutionPath $ProjectPath
    CreateModelEditorBootStrapper $SolutionPath $dxversion $OutputPath
}
catch {
    throw "$_`r`n$($_.InvocationInfo)"
}
finally {
    try {
        $mtx.ReleaseMutex() | Out-Null
        $mtx.Dispose() | Out-Null
    }
    catch {
        
    }
}



