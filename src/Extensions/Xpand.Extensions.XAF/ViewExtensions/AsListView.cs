using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions{
    public static partial class ViewExtensions{
        public static ListView AsListView(this View view) => view as ListView;
    }
}