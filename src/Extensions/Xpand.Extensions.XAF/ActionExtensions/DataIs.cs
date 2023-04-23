using DevExpress.ExpressApp.Actions;

namespace Xpand.Extensions.XAF.ActionExtensions {
    public static partial class ActionExtensions {
        public static bool DataIs(this ChoiceActionItem item, object value)
            => ReferenceEquals(item.Data, value);
    }
}