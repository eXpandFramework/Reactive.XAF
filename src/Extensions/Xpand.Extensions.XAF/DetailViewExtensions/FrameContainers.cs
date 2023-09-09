using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Templates;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.XAF.DetailViewExtensions {
    public static partial class DetailViewExtensions {
        public static T HideToolBar<T>(this T frameContainer) where T:IFrameContainer{
            ((ISupportActionsToolbarVisibility)frameContainer.Frame.Template).SetVisible(false);
            return frameContainer;
        }

        public static IEnumerable<Frame> FrameContainers(this DetailView view, params Type[] objectTypes) 
            => view.GetItems<IFrameContainer>().WhereNotDefault(container => container.Frame)
                .Where(container => objectTypes.Contains(container.Frame.View.ObjectTypeInfo.Type) )
                .Select(container => container.Frame);
    }
}