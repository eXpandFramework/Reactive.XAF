using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;

namespace Xpand.XAF.Modules.Blazor.Editors.UrlPropertyEditor {
    [PropertyEditor(typeof(string),nameof(WebPagePropertyEditor), false)]
    public class WebPagePropertyEditor : BlazorPropertyEditorBase {
        public WebPagePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) => Adapter = new WebPageEditorAdapter(new WebPageEditorModel());
        public WebPageEditorAdapter Adapter { get; }
        protected override void OnCurrentObjectChanged() {
            base.OnCurrentObjectChanged();
            Adapter.ComponentModel.Url = $"{PropertyValue}";
        }

        protected override IComponentAdapter CreateComponentAdapter() => Adapter;
    }

    public class WebPageEditorAdapter:ComponentAdapterBase<WebPageEditorModel> {
        public WebPageEditorAdapter(WebPageEditorModel webPageEditorModel) => ComponentModel=webPageEditorModel;

        protected override RenderFragment CreateComponent() 
            => builder => {
                builder.OpenElement(1, "div");
                builder.OpenElement(2, "iframe");
                builder.AddAttribute(2, "src", ComponentModel.Url);
                builder.AddAttribute(2, "width", "100%");
                builder.AddAttribute(2, "height", "800px");
                builder.CloseElement();
                builder.CloseElement();
            };

        public override WebPageEditorModel ComponentModel { get; }
    }

    public class WebPageEditorModel:ComponentModelBase {
        public string Url {
            get => GetPropertyValue<string>();
            set => SetPropertyValue(value);
        }

    }
}