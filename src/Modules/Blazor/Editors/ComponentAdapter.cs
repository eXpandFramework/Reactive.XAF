using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using Microsoft.AspNetCore.Components;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public class ComponentAdapter(Func<RenderFragment> fragmentFactory, IComponentModel componentModel) : ComponentAdapterBase {
        public override void SetAllowEdit(bool allowEdit) { }
        public override object GetValue() => null;
        public override void SetValue(object value) => DisplayTextModel.Text = $"{value}";
        public override void SetAllowNull(bool allowNull) { }
        public override void SetDisplayFormat(string displayFormat) { }
        public override void SetEditMask(string editMask) { }
        public override void SetEditMaskType(EditMaskType editMaskType) { }
        public override void SetErrorIcon(ImageInfo errorIcon) { }
        public override void SetErrorMessage(string errorMessage) { }
        public override void SetIsPassword(bool isPassword) { }
        public override void SetMaxLength(int maxLength) { }
        public override void SetNullText(string nullText) { }
        protected override RenderFragment CreateComponent() => fragmentFactory();
        public override IComponentModel ComponentModel { get; } = componentModel;
        public DxTextBoxModel DisplayTextModel { get; }=new();
    }
    
}