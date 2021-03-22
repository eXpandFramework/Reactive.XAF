using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xpand.Extensions.StringExtensions;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object), EditorAliases.MarkupContent,false)]
    public class MarkupContentPropertyEditor : ComponentPropertyEditor {
        public MarkupContentPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }

        protected override RenderFragment CreateViewComponentCore(object dataContext) => Render;

        protected override RenderFragment RenderComponent() => Render;

        private void Render(RenderTreeBuilder builder) {
            builder.AddMarkupContent(0, $"{PropertyValue}".StringFormat(Model.DisplayFormat));
        }
    }
}