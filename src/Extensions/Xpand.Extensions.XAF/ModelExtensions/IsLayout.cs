using System.Linq;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions;

public static partial class ModelExtensions {
    public static bool IsLayout(this IModelPropertyEditor modelPropertyEditor)
        => ((IModelViewItem)modelPropertyEditor).IsLayout();
    
    public static bool IsLayout(this IModelMemberViewItem modelPropertyEditor)
        => ((IModelViewItem)modelPropertyEditor).IsLayout();

    public static bool IsLayout(this IModelViewItem modelViewItem)
        => modelViewItem.GetParent<IModelDetailView>().Layout.GetItems<IModelLayoutGroup>(item => item)
            .SelectMany().OfType<IModelLayoutViewItem>().Any(element => element.ViewItem.Id == modelViewItem.Id);
}