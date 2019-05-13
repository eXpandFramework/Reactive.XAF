using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Source.Extensions.XAF.XafApplication{
    internal enum Platform{
        Agnostic,
        Win,
        Web,
        Mobile
    }

    internal static partial class XafApplicationExtensions{
        public static readonly Platform ApplicationPlatform;

        static XafApplicationExtensions(){
            var systemWebAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "System.Web");
            var httpContextType = systemWebAssembly?.Types().First(_ => _.Name == "HttpContext");
            ApplicationPlatform = httpContextType?.GetPropertyValue("Current") != null ? Platform.Web : Platform.Win;
        }
        
        internal static Platform GetPlatform(this IEnumerable<ModuleBase> moduleBases){
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
            var found = CheckPlatformCore(modules, platformStrings[0]);
            if (found){
                if (!CheckPlatformCore(modules, platformStrings[1]) &&
                    !CheckPlatformCore(modules, platformStrings[2])) return true;
                throw new NotSupportedException("Cannot load modules from different platforms");
            }

            return false;
        }

        private static bool CheckPlatformCore(ModuleBase[] moduleBases, string platformString){
            return moduleBases.Any(@base => {
                var typeInfo = XafTypesInfo.Instance.FindTypeInfo(@base.GetType());
                var attribute = typeInfo.FindAttribute<ToolboxItemFilterAttribute>();

                return attribute != null && attribute.FilterString == platformString;
            });
        }

        public static Platform GetPlatform(this DevExpress.ExpressApp.XafApplication application){
            var appNames = new[]{"WinApplication","WebApplication"};
            var baseType = application.GetType().BaseType;
            while (baseType != null &&baseType.Namespace!=null&& (!appNames.Contains(baseType.Name)&&baseType.Namespace.StartsWith("DevExpress.ExpressApp"))){
                baseType = baseType.BaseType;
            }
            return baseType?.Name=="WinApplication"?Platform.Win : Platform.Web;
        }
    }
}