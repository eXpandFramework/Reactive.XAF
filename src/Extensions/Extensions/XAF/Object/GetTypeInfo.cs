using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Source.Extensions.XAF.Object{
    internal static class ObjectExtensions{
        public static ITypeInfo GetTypeInfo(this object obj){
            return obj != null ? XafTypesInfo.Instance.FindTypeInfo(obj.GetType()) : null;
        }
    }
}