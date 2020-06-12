using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtenions{
    public static partial class ViewExtenions{
        public static ObjectView AsObjectView(this View view) => view as ObjectView;
    }
}