using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static IModelMemberViewItem[] VisibleMemberViewItems(this IModelObjectView modelObjectView){
            return modelObjectView.MemberViewItems().Where(column => !column.Index.HasValue || column.Index > -1)
                .ToArray();
        }
    }
}