using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static IModelListView GetLookupListView(this IModelMemberViewItem modelMember) => 
            (IModelListView) (modelMember.View??modelMember.Application.FindLookupListView(modelMember.ModelMember.MemberInfo.MemberType));
    }
}