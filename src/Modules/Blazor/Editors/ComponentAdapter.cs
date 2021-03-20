using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using Microsoft.AspNetCore.Components;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public class ComponentAdapter:ComponentAdapterBase {
        
        private readonly Func<RenderFragment> _fragmentFactory;
        protected ComponentAdapter() { }
        
        public ComponentAdapter(Func<RenderFragment> fragmentFactory) => _fragmentFactory = fragmentFactory;
        public override void SetAllowEdit(bool allowEdit) { }
        public override object GetValue() => null;
        public override void SetValue(object value) => DisplayTextModel.DisplayText = $"{value}";
        public override void SetAllowNull(bool allowNull) { }
        public override void SetDisplayFormat(string displayFormat) { }
        public override void SetEditMask(string editMask) { }
        public override void SetEditMaskType(EditMaskType editMaskType) { }
        public override void SetErrorIcon(ImageInfo errorIcon) { }
        public override void SetErrorMessage(string errorMessage) { }
        public override void SetIsPassword(bool isPassword) { }
        public override void SetMaxLength(int maxLength) { }
        public override void SetNullText(string nullText) { }
        protected override RenderFragment CreateComponent() => _fragmentFactory();
        public DisplayTextModel DisplayTextModel { get; }=new();
    }
    
}