using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static object DefaultMemberValue(this View view)
            => view.ObjectTypeInfo.DefaultMember?.GetValue(view.CurrentObject);
    }
}