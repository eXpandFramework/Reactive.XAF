using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static bool Available(this ActionBase actionBase) =>
            actionBase.Active.ResultValue && actionBase.Enabled.ResultValue;
    }
}