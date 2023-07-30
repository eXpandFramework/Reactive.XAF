using System.Linq;
using DevExpress.ExpressApp;

namespace Xpand.Extensions.XAF.ViewExtensions {
    public static partial class ViewExtensions {
        public static void DeleteSelectObjects(this CompositeView compositeView)
            => compositeView.SelectedObjects.Cast<object>().Do(o => compositeView.ObjectSpace.Delete(o))
                .Finally(() => compositeView.ObjectSpace.CommitChanges());
    }
}