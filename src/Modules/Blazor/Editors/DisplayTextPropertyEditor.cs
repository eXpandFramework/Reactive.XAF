using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components.Rendering;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object), nameof(DisplayTextPropertyEditor),false)]
    public class DisplayTextPropertyEditor : BlazorPropertyEditorBase {
        public DisplayTextPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }
        
        protected override void RenderComponent(RenderTreeBuilder builder) => builder.AddContent(0,PropertyValue);
    }

}