using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static IModelMemberViewItem[] OrderForView(this IEnumerable<IModelMemberViewItem> modelMemberViewItems) => modelMemberViewItems
            .OrderBy(x => x.Caption).ToArray().OrderBy(x => x.Index).ToArray();
    }
}