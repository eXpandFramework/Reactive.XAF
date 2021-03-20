using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
    public static partial class TypesInfoExtensions{
	    public static ITypeInfo ToTypeInfo(this Type type) => XafTypesInfo.Instance.FindTypeInfo(type);
	    public static IEnumerable<ITypeInfo> ToTypeInfo(this IEnumerable<Type> source) => source.Select(type => type.ToTypeInfo());
    }
}