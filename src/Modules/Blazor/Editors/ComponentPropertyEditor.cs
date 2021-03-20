                                                             using System;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(string),nameof(ComponentPropertyEditor), false)]
    public class ComponentPropertyEditor : BlazorPropertyEditorBase {
        private ComponentAdapter _componentAdapter;
        public ComponentPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) {}
        protected override IComponentAdapter CreateComponentAdapter() {
            _componentAdapter = (ComponentAdapter) base.CreateComponentAdapter();
            return _componentAdapter;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
        protected override RenderFragment RenderComponent() => ComponentModelObserver.Create(_componentAdapter.DisplayTextModel, RenderComponent);

        protected void RenderComponent(RenderTreeBuilder builder) => throw new NotImplementedException($"Subclass the {GetType().FullName} and  the {nameof(RenderComponent)} method to render your compoenent");
    }

    
}