using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.ObjectSpaceExtensions {
    public static partial class ObjectSpaceExtensions {
        public static IEnumerable<IObjectSpace> YieldAll(this IObjectSpace objectSpace)
            => objectSpace is not CompositeObjectSpace compositeObjectSpace
                ? objectSpace.YieldItem()
                : objectSpace.YieldItem().Concat(compositeObjectSpace.AdditionalObjectSpaces);
    }
}