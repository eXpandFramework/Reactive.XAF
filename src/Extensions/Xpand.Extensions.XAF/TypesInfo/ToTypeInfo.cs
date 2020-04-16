using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfo{
    public static partial class TypesInfoExtensions{
        public static ITypeInfo ToTypeInfo(this System.Type type){
            return XafTypesInfo.Instance.FindTypeInfo(type);
        }
    }
}