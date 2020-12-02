Invoke-PaketShowInstalled |Where-Object{$_.Id -match "hangfire"} 
Get-ChildItem "$nugetPackagesFolder\hangfire.Core\$hangfireVersion\lib" "Hangfire.Core.dll" -Recurse | ForEach-Object {
    $readerParams = New-Object Mono.Cecil.ReaderParameters
    $readerParams.ReadWrite = $true
    [Mono.Cecil.AssemblyDefinition]$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($_.FullName, $readerParams)
    
    if (!$asm.Name.PublicKeyToken) {
        $writeParams = New-Object Mono.Cecil.WriterParameters
        $key = [System.IO.File]::ReadAllBytes("$versionConverterPath\build\Xpand.snk")
        $writeParams.StrongNameKeyPair = [System.Reflection.StrongNameKeyPair]($key)
        $asm.Write($writeParams)
    }
    $asm.dispose()
    
}