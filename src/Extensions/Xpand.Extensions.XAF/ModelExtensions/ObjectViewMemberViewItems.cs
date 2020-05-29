using System.Linq;
using DevExpress.ExpressApp.Model;
using JetBrains.Annotations;

namespace Xpand.Extensions.XAF.ModelExtensions{
    [PublicAPI]
    public partial class ModelExtensions{
        public static IModelMemberViewItem[] MemberViewItems(this IModelObjectView modelObjectView) => (modelObjectView is IModelListView modelListView ? modelListView.Columns
                : ((IModelDetailView) modelObjectView).Items.OfType<IModelMemberViewItem>()).ToArray();

        public static IModelMemberViewItem[] MemberViewItems(this IModelObjectView modelObjectView, System.Type propertyEditorType) => modelObjectView.MemberViewItems()
                .Where(item => propertyEditorType.IsAssignableFrom(item.PropertyEditorType)).ToArray();
    }
}