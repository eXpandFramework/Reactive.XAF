using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Source.Extensions.XAF.XafApplication{
    public enum Platform{
        Agnostic,
        Win,
        Web,
        Mobile
    }

    internal static partial class XafApplicationExtensions{
        internal static Platform GetPlatform(this IEnumerable<ModuleBase> moduleBases){
            var modules = moduleBases as ModuleBase[] ?? moduleBases.ToArray();

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
            return application.Modules.GetPlatform();
        }
    }
}