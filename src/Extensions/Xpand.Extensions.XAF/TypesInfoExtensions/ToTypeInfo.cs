using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
    public static partial class TypesInfoExtensions{
	    public static ITypeInfo ToTypeInfo(this Type type) => XafTypesInfo.Instance.FindTypeInfo(type);
    }
}