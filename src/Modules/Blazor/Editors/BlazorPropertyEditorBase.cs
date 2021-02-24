using System;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public abstract class BlazorPropertyEditorBase:DevExpress.ExpressApp.Blazor.Editors.BlazorPropertyEditorBase {
        protected BlazorPropertyEditorBase(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }
        protected override IComponentAdapter CreateComponentAdapter() => new ComponentAdapter(RenderComponent);
        protected abstract RenderFragment RenderComponent();
    }
}