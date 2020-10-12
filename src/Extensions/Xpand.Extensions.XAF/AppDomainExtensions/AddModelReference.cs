using System;
using System.Linq;
using System.Reflection;
using ConcurrentCollections;
using DevExpress.ExpressApp.Utils.CodeGeneration;
using Fasterflect;
using HarmonyLib;

namespace Xpand.Extensions.XAF.AppDomainExtensions{
    public static partial class AppDomainExtensions{
	    static readonly ConcurrentHashSet<string> References=new ConcurrentHashSet<string>();
        static AppDomainExtensions(){
	        var harmony = new Harmony(typeof(TypesInfoExtensions.TypesInfoExtensions).FullName);
            var original = typeof(CSCodeCompiler).GetMethod(nameof(CSCodeCompiler.Compile));
            var prefix = typeof(AppDomainExtensions).Method(nameof(ModifyCSCodeCompilerReferences),Flags.Static|Flags.AnyVisibility);
            harmony.Patch(original, new HarmonyMethod(prefix));
        }

        public static void AddModelReference(this AppDomain appDomain, params string[] name){
	        var locations = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => name.Contains(assembly.GetName().Name)).Select(assembly => assembly.Location);
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
	        => references = references.Concat(References).Distinct().ToArray();
    }
}