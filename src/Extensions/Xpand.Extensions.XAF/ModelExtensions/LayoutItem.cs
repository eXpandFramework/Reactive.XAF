using System.Linq;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelLayoutViewItem LayoutItem(this IModelPropertyEditor modelPropertyEditor)
            => ((IModelViewItem)modelPropertyEditor).LayoutItem();
    
        public static IModelLayoutViewItem LayoutItem(this IModelMemberViewItem modelPropertyEditor)
            => ((IModelViewItem)modelPropertyEditor).LayoutItem();

        public static IModelLayoutViewItem LayoutItem(this IModelViewItem modelViewItem)
            => modelViewItem.GetParent<IModelDetailView>().Layout.GetItems<IModelLayoutGroup>(item => item)
                .SelectMany().OfType<IModelLayoutViewItem>().FirstOrDefault(element => element.ViewItem.Id == modelViewItem.Id);
    }
}