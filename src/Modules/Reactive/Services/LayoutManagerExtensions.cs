using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class LayoutManagerExtensions{
        public static IObservable<LayoutManager> WhenLayoutCreated(this LayoutManager layoutManager) 
            => layoutManager.WhenEvent(nameof(layoutManager.LayoutCreated)).To(layoutManager);
        
        public static IObservable<(IModelViewLayoutElement model,object control,ViewItem viewItem)> WhenItemCreated(this LayoutManager layoutManager) 
            => layoutManager.WhenEvent("ItemCreated").Select(p => p.EventArgs)
                .Select(e => ((IModelViewLayoutElement)e.GetPropertyValue("ModelLayoutElement"),e.GetPropertyValue("Item"),
                    (ViewItem)e.GetPropertyValue("ViewItem")));
        
        public static IObservable<CustomizeAppearanceEventArgs> WhenCustomizeAppearance(this LayoutManager layoutManager) 
            => layoutManager.WhenEvent<CustomizeAppearanceEventArgs>(nameof(ISupportAppearanceCustomization.CustomizeAppearance));
    }
}