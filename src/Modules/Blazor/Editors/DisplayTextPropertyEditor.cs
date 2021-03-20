using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Xpand.Extensions.StringExtensions;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object), EditorAliases.DisplayText,false)]
    public class DisplayTextPropertyEditor : ComponentPropertyEditor {
        public DisplayTextPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }
        

        protected override RenderFragment RenderComponent() => builder => builder.AddContent(0, PropertyValue?.StringFormat(Model.DisplayFormat));
    }
}