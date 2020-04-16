using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static IModelListView GetLookupListView(this IModelMemberViewItem modelMember){
            return (IModelListView) (modelMember.View??modelMember.Application.FindLookupListView(modelMember.ModelMember.MemberInfo.MemberType));
        }
    }
}