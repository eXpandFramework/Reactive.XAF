using System;
using System.Threading.Tasks;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Blazor.Internal.Utils;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

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
        public DisplayTextModel DisplayTextModel { get; }=new DisplayTextModel();
    }
    
    public sealed class CommonComponent : ComponentBase, IDisposable {
        private CancelableAction _pendingStateHasChanged = CancelableAction.Empty;
        public static RenderFragment Create(IComponentModel componentModel, RenderFragment childContent) => (builder) => {
            if(componentModel is null) {
                throw new ArgumentNullException(nameof(componentModel));
            }
            if(childContent is null) {
                throw new ArgumentNullException(nameof(childContent));
            }
            builder.OpenComponent<CommonComponent>(0);
            builder.AddAttribute(1, nameof(ComponentModel), componentModel);
            builder.AddAttribute(2, nameof(ChildContent), childContent);
            builder.CloseComponent();
        };
        [Parameter]
        public IComponentModel ComponentModel { get; set; }
        [Parameter]
        public RenderFragment ChildContent { get; set; }
        public override Task SetParametersAsync(ParameterView parameters) {
            Invalidate();
            return base.SetParametersAsync(parameters);
        }
        protected override void OnParametersSet() {
            base.OnParametersSet();
            if(ComponentModel != null) {
                ComponentModel.Changed += ComponentModel_Changed;
            }
        }
        private void ComponentModel_Changed(object sender, EventArgs e) {
            _pendingStateHasChanged.Cancel();
            _pendingStateHasChanged = ScheduleStateHasChanged();
        }
        private CancelableAction ScheduleStateHasChanged() {
            CancelableAction action = CancelableAction.Create(StateHasChanged);
            _ = InvokeAsync(() => action.Invoke());
            return action;
        }
        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            if(ComponentModel != null) {
                builder.AddContent(0, ChildContent);
            }
        }
        private void Invalidate() {
            if(ComponentModel != null) {
                ComponentModel.Changed -= ComponentModel_Changed;
                ComponentModel = null;
            }
            _pendingStateHasChanged.Cancel();
        }
        void IDisposable.Dispose() {
            Invalidate();
        }
    }
}