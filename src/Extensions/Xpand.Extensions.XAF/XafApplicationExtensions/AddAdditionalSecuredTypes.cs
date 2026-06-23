using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.XAF.Harmony;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public static partial class XafApplicationExtensions{
        private static MethodInfo _isSecuredTypeMethod;

        public static void AddNonSecuredType(this XafApplication application,params Type[] objectTypes){
            if (application.Security != null && application.Security.GetType().FromHierarchy(type => type.BaseType)
                    .Any(type => type.Name == "SecurityStrategy")){
                _isSecuredTypeMethod ??= application.Security.GetType().Methods().Where(info => {
                    var parameterInfos = info.Parameters();
                    return parameterInfos.Count == 1&&parameterInfos.First().ParameterType==typeof(Type)&&info.Name.EndsWith("IsSecuredType");
                }).First();
                new HarmonyMethod(typeof(XafApplicationExtensions),nameof(IsSecuredType))
                    .PreFix(_isSecuredTypeMethod,true);
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