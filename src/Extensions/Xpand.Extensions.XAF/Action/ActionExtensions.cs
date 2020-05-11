using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.Action{
    public static partial class ActionExtensions{
        public static View View(this ActionBase actionBase){
            return actionBase.View<View>();
        }

        public static T View<T>(this ActionBase actionBase) where T : View{
            return actionBase.Controller.Frame.View as T;
        }
    }
}