using System.Drawing;
using DevExpress.ExpressApp;
using Fasterflect;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static ListView HideWhenMasterDetail(this ListView listView) {
            var container = listView.LayoutManager.Container;
            var fixedPanel = container.GetPropertyValue("FixedPanel");
            fixedPanel.SetPropertyValue("Visible",false);
            var width = ((Size)fixedPanel.GetPropertyValue("MinimumSize")).Width;
            fixedPanel.SetPropertyValue("Width",width);
            return listView;
        }
    }
}