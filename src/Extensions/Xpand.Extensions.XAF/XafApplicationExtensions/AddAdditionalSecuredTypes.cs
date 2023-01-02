using System;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Harmony;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        public static void AddNonSecuredType(this XafApplication application,params Type[] objectTypes){
            if (application.Security != null && application.Security.GetType().FromHierarchy(type => type.BaseType)
                    .Any(type => type.Name == "SecurityStrategy")){
                new HarmonyMethod(typeof(XafApplicationExtensions),nameof(IsSecuredType))
                    .PreFix(application.Security.GetType().Method("IsSecuredType",Flags.Static|Flags.Public),true);
                // application.Security.GetType().Method("IsSecuredType",Flags.Static|Flags.Public)
                    // .PatchWith(new HarmonyMethod(typeof(XafApplicationExtensions),nameof(IsSecuredType)));
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