using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfo{
    public static partial class TypesInfoExtensions{
        public static ITypeInfo ToTypeInfo(this Type type){
            return XafTypesInfo.Instance.FindTypeInfo(type);
        }
    }
}