param(
    [System.IO.FileInfo]$ProjectFile
)
$moduleName = $ProjectFile.BaseName.Substring($ProjectFile.BaseName.LastIndexOf(".") + 1)
$moduleName = "$($ProjectFile.BaseName).$($moduleName)Module"
if ($moduleName -like "*.hub.*") {
    $moduleName = "Xpand.XAF.Modules.Reactive.Logger.Hub.ReactiveLoggerHubModule"
}
elseif ($moduleName -like "*Logger*") {
    $moduleName = "Xpand.XAF.Modules.Reactive.Logger.ReactiveLoggerModule"
}
$moduleName