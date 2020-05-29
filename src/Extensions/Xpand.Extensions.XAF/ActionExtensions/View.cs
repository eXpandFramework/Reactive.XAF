using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static View View(this ActionBase actionBase) => actionBase.View<View>();

        public static T View<T>(this ActionBase actionBase) where T : View => actionBase.Controller.Frame.View as T;
    }
}