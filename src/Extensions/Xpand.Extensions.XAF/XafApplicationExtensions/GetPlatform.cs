using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using HarmonyLib;

namespace Xpand.Extensions.XAF.XafApplicationExtensions{
    public enum Platform{
        Agnostic,
        Win,
        Web,
        Mobile,
        Blazor
    }

    public static partial class XafApplicationExtensions{
        private static Harmony _harmony;
        private static ConcurrentBag<Type> _securedTypes;
        static XafApplicationExtensions() => Init();

        private static void Init(){
            _securedTypes=new ConcurrentBag<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var systemWebAssembly = assemblies.FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
            var httpContextType = systemWebAssembly?.Types().First(_ => _.Name == "HttpContext");
            IsHosted = httpContextType?.GetPropertyValue("Current") != null;
            _harmony = new Harmony(typeof(XafApplicationExtensions).Namespace);
        }

        public static bool IsHosted{ get; set; }

        public static Platform GetPlatform(this IEnumerable<ModuleBase> moduleBases){
            var modules = moduleBases as ModuleBase[] ?? moduleBases.ToArray();
            var application = modules.Select(_ => _.Application).FirstOrDefault(_ => _!=null);
            if (application != null){
                return application.GetPlatform();
            }
            var webPlatformString = "Xaf.Platform.Web";
            var winPlatformString = "Xaf.Platform.Win";
            var mobilePlatformString = "Xaf.Platform.Mobile";

            if (CheckPlatform(modules, webPlatformString, winPlatformString, mobilePlatformString))
                return Platform.Web;
            if (CheckPlatform(modules, winPlatformString, webPlatformString, mobilePlatformString))
                return Platform.Win;
            if (CheckPlatform(modules, mobilePlatformString, webPlatformString, winPlatformString))
                return Platform.Mobile;
            return Platform.Agnostic;
        }

        private static bool CheckPlatform(ModuleBase[] modules, params string[] platformStrings){
            if (CheckPlatformCore(modules, platformStrings[0])){
                if (!CheckPlatformCore(modules, platformStrings[1]) &&
                    !CheckPlatformCore(modules, platformStrings[2])) return true;
                throw new NotSupportedException("Cannot load modules from different platforms");
            }
            return false;
        }

        private static bool CheckPlatformCore(ModuleBase[] moduleBases, string platformString) =>
            moduleBases.Any(@base => {
                var typeInfo = XafTypesInfo.Instance.FindTypeInfo(@base.GetType());
                var attribute = typeInfo.FindAttribute<ToolboxItemFilterAttribute>();

                return attribute != null && attribute.FilterString == platformString;
            });

        public static Platform GetPlatform(this XafApplication application){
            var appNames = new[]{"WinApplication","WebApplication","BlazorApplication"};
            var baseType = application.GetType().BaseType;
            while (baseType != null &&baseType.Namespace!=null&& (!appNames.Contains(baseType.Name)&&!baseType.Namespace.StartsWith("DevExpress.ExpressApp"))){
                baseType = baseType.BaseType;
            }

            return baseType?.Name switch{
	            "WinApplication" => Platform.Win,
	            "WebApplication" => Platform.Web,
	            "BlazorApplication" => Platform.Blazor,
	            _ => throw new NotImplementedException(application.GetType().FullName)
            };
        }
    }
}