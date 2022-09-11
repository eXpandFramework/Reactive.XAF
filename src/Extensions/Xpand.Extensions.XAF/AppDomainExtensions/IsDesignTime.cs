using System;
using DevExpress.ExpressApp.Model.Core;

namespace Xpand.Extensions.XAF.AppDomainExtensions {
    public static partial class AppDomainExtensions {
        public static bool IsDesignTime(this AppDomain appDomain, params string[] name) 
            => DesignerOnlyCalculator.IsRunFromDesigner;
        public static bool IsRunTime(this AppDomain appDomain, params string[] name) 
            => !DesignerOnlyCalculator.IsRunFromDesigner;
    }
}