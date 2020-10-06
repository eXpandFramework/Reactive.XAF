using System;
using System.Linq;
using DevExpress.ExpressApp.DC;

namespace Xpand.Extensions.XAF.TypesInfoExtensions{
	public static partial class TypesInfoExtensions{
        public static Version XAFVersion(this ITypesInfo typesInfo) 
            => typeof(TypesInfoExtensions).Assembly.GetReferencedAssemblies().First(_ => _.Name.Contains("DevExpress")).Version;
	}
}