using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components.Rendering;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(string),nameof(FrameWebPagePropertyEditor), false)]
    public class FrameWebPagePropertyEditor : BlazorPropertyEditorBase {
        public FrameWebPagePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) {}

        protected override void RenderComponent(RenderTreeBuilder builder) 
            => builder.AddMarkupContent(0,$"<div><iframe src='{PropertyValue}' witdth='100%' height='100%'></div></iframe>");
    }
}