using System.Drawing;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static ListView HideWhenMasterDetail(this ListView listView) {
            var container = listView.LayoutManager.Container;
            var fixedPanel = container.GetPropertyValue("FixedPanel");
            fixedPanel.SetPropertyValue("Visible",false);
            var width = fixedPanel.GetPropertyValue("MinimumSize").To<Size>().Width;
            fixedPanel.SetPropertyValue("Width",width);
            return listView;
        }
    }
}