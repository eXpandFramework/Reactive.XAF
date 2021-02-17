using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Xpand.XAF.Modules.Blazor.Editors {
    public class ComponentAdapter:DevExpress.ExpressApp.Blazor.Editors.Adapters.ComponentAdapterBase {
        private readonly Action<RenderTreeBuilder> _buildFragment;
        protected ComponentAdapter() { }
        public ComponentAdapter(Action<RenderTreeBuilder> buildFragment) => _buildFragment = buildFragment;
        public override void SetAllowEdit(bool allowEdit) { }
        public override object GetValue() => null;
        public override void SetValue(object value) { }
        public override void SetAllowNull(bool allowNull) { }
        public override void SetDisplayFormat(string displayFormat) { }
        public override void SetEditMask(string editMask) { }
        public override void SetEditMaskType(EditMaskType editMaskType) { }
        public override void SetErrorIcon(ImageInfo errorIcon) { }
        public override void SetErrorMessage(string errorMessage) { }
        public override void SetIsPassword(bool isPassword) { }
        public override void SetMaxLength(int maxLength) { }
        public override void SetNullText(string nullText) { }
        protected override RenderFragment CreateComponent() => builder =>_buildFragment?.Invoke(builder) ;
    }
    public abstract class ComponentAdapter<TComponentModel> :ComponentAdapter where TComponentModel:ComponentModelBase{
        public abstract TComponentModel ComponentModel { get; }
    }
}