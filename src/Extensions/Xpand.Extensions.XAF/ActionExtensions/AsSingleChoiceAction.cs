using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static SingleChoiceAction AsSingleChoiceAction(this ActionBase action) => action as SingleChoiceAction;
    }
}