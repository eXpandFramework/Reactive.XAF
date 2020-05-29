using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Web.UI.WebControls;
using DevExpress.Data.Extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Editors;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Web.TestScripts;
using DevExpress.ExpressApp.Web.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Web;
using Xpand.Extensions.StringExtensions;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.LookupCascade{
    [PropertyEditor(typeof(object), false)]
    public class ASPxLookupCascadePropertyEditor :ASPxObjectPropertyEditorBase, IDependentPropertyEditor,IParentViewPropertyEditor{
        
        
        private IModelMemberViewItem[] _lookupViewModelMemberViewItems;
        private string _clientStorage;
        private WebLookupEditorHelper _helper;

        public ASPxLookupCascadePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model){
        }
        
        protected override WebControl CreateEditModeControlCore(){
            _comboBox = RenderHelper.CreateASPxComboBox();
            _comboBox.Width = Unit.Percentage(100);
            _comboBox.ValueChanged+=ComboBoxOnValueChanged;
            _comboBox.TextFormatString = EditorModel.TextFormatString;
            _comboBox.AllowNull = true;
            _comboBox.NullText = LookupCascadeService.NA;
            ConfigureColumns(_comboBox);
            ConfigureSynchronize(_comboBox);
            ConfigureCascade(_comboBox);
            ConfigureClientSideEvents(_comboBox);
            return _comboBox;
        }

        protected override string GetTestCaption() {
            return Model.Caption;
        }

        private void ComboBoxOnValueChanged(object sender, EventArgs e){
            PropertyValue = _helper.GetObjectByKey(CurrentObject, $"{_comboBox.Value}");
            WriteValue();
        }

        protected override IJScriptTestControl GetEditorTestControlImpl(){
            return new JSASPxComboBoxTestControl();
        }

        protected override object GetControlValueCore(){
            return PropertyValue;
        }

        private IModelLookupCascadePropertyEditor EditorModel => ((IModelMemberViewItemLookupCascadePropertyEditor) Model).LookupCascade;

        void IParentViewPropertyEditor.SetParentView(ObjectView value){
            _parentView = value;
            if (value is ListView listView){
                var gridListEditor = ((ASPxGridListEditor) listView.Editor);
                
                ClientSideEventsHelper.AssignClientHandlerSafe(gridListEditor.Grid,
                    nameof(gridListEditor.Grid.ClientSideEvents.BeginCallback),
                    $"function(s,e){{ClearEditorItems('{_clientStorage}','{View}')}}",
                    $"{GetType().FullName}{nameof(gridListEditor.Grid.ClientSideEvents.BeginCallback)}");
            }
        }

        protected override string GetPropertyDisplayValue(){
            return _helper.GetEscapedDisplayText(PropertyValue,NullText,DisplayFormat);
        }

        public override void Setup(IObjectSpace space, XafApplication xafApplication){
            base.Setup(space, xafApplication);
            var modelClientDatasource = xafApplication.ReactiveModulesModel().LookupCascadeModel().Select(_ => _.ClientDatasource).Wait();
            _helper = new WebLookupEditorHelper(xafApplication, space, MemberInfo.MemberTypeInfo, Model){
                EditorMode = LookupEditorMode.AllItems
            };
            ImmediatePostData = false;
            var lookupListViewModel = _helper.LookupListViewModel;
            _lookupViewModelMemberViewItems = lookupListViewModel.VisibleMemberViewItems().OrderForView();
            _clientStorage = modelClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
        }

        private void ConfigureClientSideEvents(ASPxComboBox comboBox){
            var displayText = _helper.GetDisplayText(PropertyValue,NullText,DisplayFormat);
            var objectKey = _helper.GetObjectKey(PropertyValue);
            comboBox.ClientSideEvents.Init = $"function(s,e){{EditorInit(s,'{_helper.LookupListViewModel.Id}','{_parentView.Id}','{_clientStorage}','{displayText}','{objectKey}');}}";
        }

        private void ConfigureColumns(ASPxComboBox comboBox){
            comboBox.Columns.Clear();
            foreach (var memberViewItem in _lookupViewModelMemberViewItems){
                var listBoxColumn = comboBox.Columns.Add(memberViewItem.Id);
                listBoxColumn.Caption = memberViewItem.Caption;
                var clientVisible = ((IModelColumnClientVisible) memberViewItem).ClientVisible;
                if (clientVisible.HasValue){
                    listBoxColumn.ClientVisible = clientVisible.Value;
                }
            }
        }

        private void ConfigureSynchronize(ASPxComboBox comboBox){
            var lookupCascadePropertyEditor = Model.GetParent<IModelObjectView>().VisibleMemberViewItems()
                .Cast<IModelMemberViewItemLookupCascadePropertyEditor>().FirstOrDefault(_ => _.LookupCascade.CascadeMemberViewItem == Model && _.LookupCascade.Synchronize);
            if (lookupCascadePropertyEditor!=null){
                var filterColumnIndex = comboBox.Columns.OfType<ListBoxColumn>().ToArray().FindIndex(column => column.FieldName==lookupCascadePropertyEditor.LookupCascade.CascadeColumnFilter.Id);
                var cascadeMember = lookupCascadePropertyEditor.GetParent<IModelMemberViewItem>();
                ClientSideEventsHelper.AssignClientHandlerSafe(comboBox, nameof(comboBox.ClientSideEvents.ValueChanged),
                    $"function(s,e){{SynchronizeLookupValue(s,'{cascadeMember.GetLookupListView().Id}','{_parentView.Id}','{filterColumnIndex}','{cascadeMember.ModelMember.Type.FullName}')}}",
                    $"{GetType().FullName}{nameof(ConfigureSynchronize)}{nameof(comboBox.ClientSideEvents.ValueChanged)}");    
            }
        }

        private void ConfigureCascade(ASPxComboBox comboBox){
            var filterLookup = FilterLookupScript();
            if (filterLookup != null){
                ClientSideEventsHelper.AssignClientHandlerSafe(comboBox, nameof(comboBox.ClientSideEvents.ValueChanged),
                    $"function(s,e){{{filterLookup}}}", $"{GetType().FullName}{nameof(ConfigureCascade)}{nameof(comboBox.ClientSideEvents.ValueChanged)}");
            }
        }

        private string FilterLookupScript(){
            var cascadeMemberViewItem = EditorModel.CascadeMemberViewItem;
            if (cascadeMemberViewItem != null){
                var cascadeViewId = cascadeMemberViewItem.GetLookupListView().Id;
                return  $"FilterLookup(s,'{_clientStorage}','{cascadeViewId}','{EditorModel.CascadeColumnFilter.Caption}','{_parentView.Id}')";
            }

            return null;
        }

        private ASPxComboBox _comboBox;
        private ObjectView _parentView;
        

        IList<string> IDependentPropertyEditor.MasterProperties => _helper.MasterProperties;

    }

}