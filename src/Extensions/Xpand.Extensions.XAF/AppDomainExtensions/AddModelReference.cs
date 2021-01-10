using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ConcurrentCollections;
using DevExpress.ExpressApp.Utils.CodeGeneration;
using Fasterflect;
using HarmonyLib;
using JetBrains.Annotations;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.AppDomainExtensions{
    public static partial class AppDomainExtensions{
	    static readonly ConcurrentHashSet<string> References=new ConcurrentHashSet<string>();
        static AppDomainExtensions(){
	        AppDomain.CurrentDomain.Patch(harmony => {
		        var original = typeof(CSCodeCompiler).GetMethod(nameof(CSCodeCompiler.Compile));
		        var prefix = typeof(AppDomainExtensions).Method(nameof(ModifyCSCodeCompilerReferences),Flags.Static|Flags.AnyVisibility);
		        harmony.Patch(original, new HarmonyMethod(prefix));
	        });
        }

        [PublicAPI]
        public static void AddModelReference(this AppDomain appDomain, params string[] name){
	        var locations = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => name.Contains(assembly.GetName().Name))
                .Select(assembly => assembly.Location);
	        foreach (var location in locations){
		        References.Add(location);
	        }
        }

        [PublicAPI]
        public static void AddModelReference(this AppDomain appDomain, params Assembly[] assemblies){
	        foreach (var assembly in assemblies){
		        References.Add(assembly.Location);
	        }
        }

        [UsedImplicitly]
        internal static void ModifyCSCodeCompilerReferences(string sourceCode, ref string[] references, string assemblyFile) 
	        => references = references.Concat(References).DistinctBy(Path.GetFileName).ToArray();
    }
}