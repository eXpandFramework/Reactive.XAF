using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public partial class ModelExtensions{
        public static IEnumerable<IModelCommonMemberViewItem> CommonMemberViewItems(this IModelObjectView modelObjectView,string id=null){
            var viewItems = modelObjectView is IModelDetailView modelDetailView
                ? modelDetailView.Items.OfType<IModelCommonMemberViewItem>()
                : ((IModelListView) modelObjectView).Columns;
            return id != null ? viewItems.Cast<IModelNode>().Where(item => item.Id() == id).Cast<IModelCommonMemberViewItem>() : viewItems;
        }
    }
}