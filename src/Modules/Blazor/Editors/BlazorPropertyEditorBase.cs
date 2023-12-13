using System;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public abstract class BlazorPropertyEditorBase(Type objectType, IModelMemberViewItem model)
        : DevExpress.ExpressApp.Blazor.Editors.BlazorPropertyEditorBase(objectType, model) {
        protected override IComponentAdapter CreateComponentAdapter() => new ComponentAdapter(RenderComponent,ComponentModel);
        
        protected abstract RenderFragment RenderComponent();
        
    }
}