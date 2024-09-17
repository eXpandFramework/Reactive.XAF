using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using Fasterflect;
using Xpand.Extensions.XAF.CriteriaOperatorExtensions;
using Xpand.Extensions.XAF.TypesInfoExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static T[] Query<T>(this IObjectSpace objectSpace, Expression<Func<T, bool>> criteria = null)
            => objectSpace.TypesInfo.DomainComponents(typeof(T))
                .SelectMany(type => objectSpace.GetObjects(type, criteria.ToCriteria()).Cast<T>()).ToArray();
        
        public static IObjectSpace AdditionalObjectSpace(this IObjectSpace objectSpace, Type type)
            => (IObjectSpace)objectSpace.CallMethod("GetCertainObjectSpace", type);
    }
}