using System;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.Data.Linq.Helpers;
using DevExpress.Data.ODataLinq.Helpers;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Fasterflect;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static bool CanCreateObject(this IObjectSpace objectSpace, ITypeInfo type) => objectSpace.CanCreateObject(type.Type);

        public static bool CanCreateObject(this IObjectSpace objectSpace, Type type) {
            var constructorInfo = objectSpace.TypesInfo.FindTypeInfo(type).Type.Constructor(Flags.Public | Flags.Instance);
            return constructorInfo != null && constructorInfo.IsPublic && constructorInfo.GetParameters().Length == 0;
        }
    }
}