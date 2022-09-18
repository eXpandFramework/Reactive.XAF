using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.DetailViewExtensions {
    public static partial class DetailViewExtensions {
        public static IEnumerable<Frame> FrameContainers(this DetailView view, Type objectType) 
            => view.GetItems<IFrameContainer>().WhereNotDefault(container => container.Frame)
                .Where(container => container.Frame.View.ObjectTypeInfo.Type == objectType)
                .Select(container => container.Frame);
    }
}