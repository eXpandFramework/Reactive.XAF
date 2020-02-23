using namespace Mono.Cecil;
class AssemblyResolver : DefaultAssemblyResolver{
    [System.IO.FileInfo[]] $Assemblies
    [AssemblyDefinition[]] $ResolvedDefinitions=@()
    AssemblyResolver([String] $Path) {
        $this.assemblies=Get-ChildItem $Path *.dll -Recurse
    }
    AssemblyResolver([System.IO.FileInfo[]] $AssemblyList) {
        $this.assemblies=$AssemblyList
    }
    
    [void] Dispose([bool]$disposing){
        $this.resolvedDefinitions|ForEach-Object{$_.Dispose()}
        ([DefaultAssemblyResolver]$this).Dispose($disposing)
    }
    [AssemblyDefinition] Resolve([AssemblyNameReference]$name, [ReaderParameters]$parameters){
        try {
            return ([DefaultAssemblyResolver]$this).Resolve($name,$parameters)
        }
        catch {
            $assemblyName=$name.Name
            $comma=$assemblyName.IndexOf(",")
            if ($comma -gt -1)     {
                $assemblyName=$assemblyName.Substring(0,$comma)
            }
            $assembly=($this.assemblies|Where-Object{$_.Name -eq "$assemblyName.dll"}).FullName|Select-Object -First 1
            $definition=[AssemblyDefinition]::ReadAssembly($assembly)
            $this.resolvedDefinitions+=$definition
            return $definition
        }
    }
}