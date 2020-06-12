using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtenions{
    public static partial class ViewExtenions{
        public static ListView AsListView(this View view) => view as ListView;
    }
}