using System;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;

namespace Xpand.XAF.Modules.Blazor.Editors.DisplayText {
    [PropertyEditor(typeof(object), nameof(DisplayTextPropertyEditor),false)]
    public class DisplayTextPropertyEditor : BlazorPropertyEditorBase {
        public DisplayTextPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) { }
        protected override IComponentAdapter CreateComponentAdapter() => new DisplayTextAdapter(new DisplayTextModel());
    }

    public class DisplayTextAdapter : ComponentAdapterBase<DisplayTextModel> {
        public DisplayTextAdapter(DisplayTextModel componentModel) => ComponentModel = componentModel;
        public override object GetValue() => ComponentModel.DisplayText;
        public override void SetValue(object value) => ComponentModel.DisplayText = $"{value}";
        public override DisplayTextModel ComponentModel { get; }
        protected override RenderFragment CreateComponent() => ComponentModelObserver.Create(ComponentModel, DisplayTextRenderer.Create(ComponentModel));
    }
}