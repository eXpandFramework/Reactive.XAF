using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static IModelMemberViewItem[] VisibleMemberViewItems(this IModelObjectView modelObjectView){
            return modelObjectView.MemberViewItems().VisibleMemberViewItems().ToArray();
        }
        public static IModelMemberViewItem[] VisibleMemberViewItems(this IEnumerable<IModelMemberViewItem> modelMemberViewItems){
            return modelMemberViewItems.Where(item => !item.Index.HasValue || item.Index > -1).ToArray();
        }
    }
}