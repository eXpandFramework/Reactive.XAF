using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions{
    public static partial class ViewExtensions{
        public static ObjectView AsObjectView(this View view) => view as ObjectView;
    }
}