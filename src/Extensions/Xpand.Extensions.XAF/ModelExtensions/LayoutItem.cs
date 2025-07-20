using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ModelExtensions {
    public static partial class ModelExtensions {
        public static IModelLayoutViewItem LayoutItem(this IModelPropertyEditor modelPropertyEditor)
            => ((IModelViewItem)modelPropertyEditor).LayoutItem();
    
        public static IModelLayoutViewItem LayoutItem(this IModelMemberViewItem modelPropertyEditor)
            => ((IModelViewItem)modelPropertyEditor).LayoutItem();

        public static IEnumerable<IModelViewLayoutElement> Flatten(this IModelViewLayout viewLayout) 
            => viewLayout.OfType<IModelLayoutGroup>()
                .SelectMany(group => group.SelectManyRecursive(element => element as IEnumerable<IModelViewLayoutElement>));

        public static IModelLayoutViewItem LayoutItem(this IModelViewItem modelViewItem)
            => modelViewItem.GetParent<IModelDetailView>().Layout.Flatten()
                .OfType<IModelLayoutViewItem>().FirstOrDefault(element => element.ViewItem.Id == modelViewItem.Id);
        
        
    }
}