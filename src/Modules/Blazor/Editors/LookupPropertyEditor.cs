using System;
using System.Reactive.Linq;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object),EditorAliases.BlazorLookup,false)]
    public class LookupPropertyEditor:DevExpress.ExpressApp.Blazor.Editors.LookupPropertyEditor {
        public LookupPropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model) {
        }

        public override bool CanFormatPropertyValue => true;
        // protected override IComponentAdapter CreateComponentAdapter() {
        //     var componentAdapter = base.CreateComponentAdapter();
        //     ((ObjectString) CurrentObject).DataSource.WhenObjects()
        //         .Do(o => ((DxComboBoxAdapter<object>) componentAdapter).ComponentModel.Data)
        //     return componentAdapter;
        // }
    }
}