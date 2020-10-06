using System;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Utils.CodeGeneration;
using Fasterflect;
using HarmonyLib;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
    public static partial class TypesInfoExtensions{
        private static readonly Harmony Harmony;
        private static string _netStandardLocation;

        static TypesInfoExtensions(){
            Harmony = new Harmony(typeof(TypesInfoExtensions).FullName);
        }
        public static void ReferenceNetStandard(this ITypesInfo typesInfo){
            _netStandardLocation = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name.Contains("netstandard"))?.Location;
            var original = typeof(CSCodeCompiler).GetMethod(nameof(CSCodeCompiler.Compile));
            if (_netStandardLocation != null&& Harmony.GetAllPatchedMethods().All(m => m != original)){
                var prefix = typeof(TypesInfoExtensions).Method(nameof(ModifyCSCodeCompilerReferences),Flags.Static|Flags.AnyVisibility);
                Harmony.Patch(original, new HarmonyMethod(prefix));
            }
        }

        internal static void ModifyCSCodeCompilerReferences(string sourceCode, ref string[] references, string assemblyFile) {
            var fileName = $"{Path.GetFileName(_netStandardLocation)}";
            if (!references.Any(s => fileName.Equals(Path.GetFileName(s),StringComparison.OrdinalIgnoreCase))){
                references = references.Concat(new[]{_netStandardLocation}).ToArray();
            }
        }

    }
}