using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions{
    public static partial class ActionExtensions{
        public static SimpleAction AsSimpleAction(this ActionBase action) => action as SimpleAction;
        public static SimpleAction ToSimpleAction(this ActionBase action) => ((SimpleAction)action);
        
        public static ParametrizedAction AsParametrizedAction(this ActionBase action) 
            => action as ParametrizedAction;
        public static ParametrizedAction ToParametrizedAction(this ActionBase action) 
            => (ParametrizedAction)action;
        
        public static PopupWindowShowAction AsPopupWindowShowAction(this ActionBase action) => action as PopupWindowShowAction;
        
        public static SingleChoiceAction AsSingleChoiceAction(this ActionBase action) => action as SingleChoiceAction;
        public static SingleChoiceAction ToSingleChoiceAction(this ActionBase action) => (SingleChoiceAction)action;
    }
}