using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static SimpleAction AsSimpleAction(this ActionBase action) => action as SimpleAction;
        public static SimpleAction ToSimpleAction(this ActionBase action) => ((SimpleAction)action);
    }
}