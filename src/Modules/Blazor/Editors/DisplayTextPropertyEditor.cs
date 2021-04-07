﻿using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object), EditorAliases.DisplayText,false)]
    public class DisplayTextPropertyEditor : ComponentPropertyEditor {
        public DisplayTextPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }
        protected override RenderFragment CreateViewComponentCore(object dataContext) {
            return builder => builder.AddContent(0, this.DisplayableMemberValue(dataContext,dataContext)?.StringFormat(Model.DisplayFormat));
        }

        protected override RenderFragment RenderComponent() => builder => builder.AddContent(0, this.DisplayableMemberValue()?.StringFormat(Model.DisplayFormat));

    }
    
}