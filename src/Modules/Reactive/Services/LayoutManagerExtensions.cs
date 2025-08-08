using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model;
using Fasterflect;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;

namespace Xpand.XAF.Modules.Reactive.Services{
    public static class LayoutManagerExtensions{
        public static IObservable<LayoutManager> WhenLayoutCreated(this LayoutManager layoutManager) 
            => layoutManager.ProcessEvent(nameof(layoutManager.LayoutCreated)).To(layoutManager);
        
        public static IObservable<(IModelViewLayoutElement model,object control,ViewItem viewItem)> WhenItemCreated(this LayoutManager layoutManager) 
            => layoutManager.ProcessEvent("ItemCreated")
                .Select(e =>layoutManager.GetType().Name.StartsWith("Blazor")? e.LayoutItem("LayoutControlItem"):e.LayoutItem(("Item")));

        private static (IModelViewLayoutElement, object, ViewItem) LayoutItem(this object e,string itemPropertyName) 
            => ((IModelViewLayoutElement)e.GetPropertyValue("ModelLayoutElement"),e.GetPropertyValue(itemPropertyName), (ViewItem)e.GetPropertyValue("ViewItem"));

        public static Platform Platform(this LayoutManager layoutManager)
            => layoutManager.GetType().Name.StartsWith("Blazor")?Xpand.Extensions.XAF.XafApplicationExtensions.Platform.Blazor : Xpand.Extensions.XAF.XafApplicationExtensions.Platform.Win;
        
        public static IObservable<CustomizeAppearanceEventArgs> WhenCustomizeAppearance(this LayoutManager layoutManager) 
            => layoutManager.ProcessEvent<CustomizeAppearanceEventArgs>(nameof(ISupportAppearanceCustomization.CustomizeAppearance));
    }
}