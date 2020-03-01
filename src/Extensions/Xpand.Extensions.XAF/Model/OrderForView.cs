using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static IModelMemberViewItem[] OrderForView(this IEnumerable<IModelMemberViewItem> modelMemberViewItems){
            var orderByCaption = modelMemberViewItems.OrderBy(x => x.Caption).ToArray();
            var orderbyIndex = orderByCaption.OrderBy(x => x.Index).ToArray();
            return orderbyIndex;
        }
    }
}