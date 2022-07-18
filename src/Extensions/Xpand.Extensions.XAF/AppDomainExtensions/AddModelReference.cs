using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ConcurrentCollections;
using DevExpress.ExpressApp.Utils.CodeGeneration;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Harmony;

namespace Xpand.Extensions.XAF.AppDomainExtensions{
    public static partial class AppDomainExtensions{
	    static readonly ConcurrentHashSet<string> References=new();
        static AppDomainExtensions() 
	        => new HarmonyMethod(typeof(AppDomainExtensions).Method(nameof(ModifyCSCodeCompilerReferences),Flags.Static|Flags.AnyVisibility))
		        .PreFix(typeof(CSCodeCompiler).GetMethod(nameof(CSCodeCompiler.Compile)),true);

        
        public static void AddModelReference(this AppDomain appDomain, params string[] name){
	        var locations = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => name.Contains(assembly.GetName().Name))
                .Select(assembly => assembly.Location);
	        foreach (var location in locations){
		        References.Add(location);
	        }
        }

        
        public static void AddModelReference(this AppDomain appDomain, params Assembly[] assemblies){
	        foreach (var assembly in assemblies){
		        References.Add(assembly.Location);
	        }
        }

        
        internal static void ModifyCSCodeCompilerReferences(string sourceCode, ref string[] references, string assemblyFile) 
	        => references = references.Concat(References).DistinctWith(Path.GetFileName).ToArray();
    }
}