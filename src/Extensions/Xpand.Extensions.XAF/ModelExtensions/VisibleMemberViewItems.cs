using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static IModelMemberViewItem[] VisibleMemberViewItems(this IModelObjectView modelObjectView) 
            => modelObjectView.MemberViewItems().VisibleMemberViewItems().ToArray();

        public static IModelMemberViewItem[] VisibleMemberViewItems(this IEnumerable<IModelMemberViewItem> modelMemberViewItems) 
            => modelMemberViewItems.Where(item => item.Index is null or > -1).ToArray();
    }
}