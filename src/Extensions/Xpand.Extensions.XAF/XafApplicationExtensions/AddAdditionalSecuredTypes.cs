using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static void AddNonSecuredType(this XafApplication application,params Type[] objectTypes){
            if (application.Security != null && application.Security.GetType().FromHierarchy(type => type.BaseType)
                    .Any(type => type.Name == "SecurityStrategy")){
                
                var isSecuredTypeMethod = application.Security.GetType().Method("IsSecuredType",Flags.Static|Flags.Public);
                var postfix = new HarmonyMethod(typeof(XafApplicationExtensions).Method(nameof(IsSecuredType),Flags.Static|Flags.NonPublic));
                _harmony.Patch(isSecuredTypeMethod,postfix);

                foreach (var securedType in objectTypes){
                    _securedTypes.Add(securedType);   
                }
            }
        }

        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool IsSecuredType(ref bool __result, Type type){
            if (_securedTypes.Contains(type)){
                __result = false;
                return false;
            }

            return true;
        }
    }
}