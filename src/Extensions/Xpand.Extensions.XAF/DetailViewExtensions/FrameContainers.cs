using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.DetailViewExtensions {
    public static partial class DetailViewExtensions {
        public static IEnumerable<Frame> FrameContainers(this DetailView view, params Type[] objectTypes) 
            => view.GetItems<IFrameContainer>().WhereNotDefault(container => container.Frame)
                .Where(container => objectTypes.Contains(container.Frame.View.ObjectTypeInfo.Type) )
                .Select(container => container.Frame);
    }
}