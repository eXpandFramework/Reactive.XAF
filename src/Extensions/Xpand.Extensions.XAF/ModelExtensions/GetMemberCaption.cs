using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.ModelExtensions{
    public static partial class ModelExtensions{
        public static string GetMemberCaption(this IModelClass modelClass, string memberName) => 
            modelClass.FindMember(memberName)?.Caption ?? modelClass.TypeInfo.FindMember(memberName).Name;
    }
}