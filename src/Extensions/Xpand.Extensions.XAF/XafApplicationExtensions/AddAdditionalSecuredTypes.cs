using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Harmony;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        
        public static int Count<T>(this XafApplication application, Expression<Func<T,bool>> expression=null) where T:class {
            using var objectSpace = application.CreateObjectSpace();
            return objectSpace.GetObjectsQuery<T>().Count(expression ?? (arg => true));
        }

        public static T Module<T>(this XafApplication application) where T:ModuleBase => application.Modules.OfType<T>().FirstOrDefault();

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