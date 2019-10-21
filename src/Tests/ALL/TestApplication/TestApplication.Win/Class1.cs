using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;

public class MyDefaultAssemblyResolver : DefaultAssemblyResolver{
    List<AssemblyDefinition> _resolvedDefinitions=new List<AssemblyDefinition>();
    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters){
        var definition = ResolveAssemblyDefinition(name, parameters);
        _resolvedDefinitions.Add(definition);
        return definition;
    }

    private AssemblyDefinition ResolveAssemblyDefinition(AssemblyNameReference name, ReaderParameters parameters){
        try{
            return base.Resolve(name, parameters);
        }
        catch (AssemblyResolutionException){
            return AssemblyDefinition(name);
        }
    }

    protected override void Dispose(bool disposing){
        base.Dispose(disposing);
        foreach (var resolvedDefinition in _resolvedDefinitions){
            resolvedDefinition.Dispose();
        }
    }

    private static AssemblyDefinition AssemblyDefinition(AssemblyNameReference name){
        var assemblies = Directory.GetFiles(@"$packagesFolder", string.Format("{0}.dll", name.Name),
            SearchOption.AllDirectories);
        foreach (var assembly in assemblies){
            var fileVersion = new Version(FileVersionInfo.GetVersionInfo(assembly).FileVersion);
            if (fileVersion == name.Version){
                return Mono.Cecil.AssemblyDefinition.ReadAssembly(assembly);
            }
        }

        return Mono.Cecil.AssemblyDefinition.ReadAssembly(string.Format(@"$resolvePath\{0}.dll", name.Name));
    }
}