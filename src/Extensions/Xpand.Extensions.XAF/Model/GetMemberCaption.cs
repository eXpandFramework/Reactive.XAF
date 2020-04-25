using DevExpress.ExpressApp.Model;

namespace Xpand.Extensions.XAF.Model{
    public static partial class ModelExtensions{
        public static string GetMemberCaption(this IModelClass modelClass, string memberName){
            return modelClass.FindMember(memberName)?.Caption ?? modelClass.TypeInfo.FindMember(memberName).Name;
        }
    }
}