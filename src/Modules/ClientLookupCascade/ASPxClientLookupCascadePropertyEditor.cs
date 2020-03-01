using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Web.Editors;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Web.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Web;
using Xpand.Extensions.String;
using Xpand.Extensions.XAF.Model;

namespace Xpand.XAF.Modules.ClientLookupCascade{
    [PropertyEditor(typeof(object), false)]
    public class ASPxClientLookupCascadePropertyEditor :ASPxObjectPropertyEditorBase, IDependentPropertyEditor{
        private string _uniqueID;
        
        private IModelMemberViewItem[] _lookupViewModelMemberViewItems;
        private string _clientStorage;
        private WebLookupEditorHelper _helper;

        public ASPxClientLookupCascadePropertyEditor(Type objectType, IModelMemberViewItem model) : base(objectType, model){
        }
        
        protected override WebControl CreateEditModeControlCore(){
            Panel panel = new Panel{Width = Unit.Percentage(100)};
            _comboBox = new ASPxComboBox{Width = Unit.Percentage(100)};
            _comboBox.ValueChanged+=ComboBoxOnValueChanged;
            panel.Controls.Add(_comboBox);
            
            
            _uniqueID = _helper.LookupListViewModel.GetUniqueID(ParentView.Id);
            
            _comboBox.ClientInstanceName = _uniqueID;
            _comboBox.TextFormatString = EditorModel.TextFormatString;
            _comboBox.AllowNull = true;
            _comboBox.NullText = "N/A";
            ConfigureSynchronize(_comboBox);
            ConfigureCascade(_comboBox);
            ConfigureClientSideEvents(_comboBox);
            ConfigureColumns(_comboBox);
            return panel;
        }

        private void ComboBoxOnValueChanged(object sender, EventArgs e){
            PropertyValue = _helper.GetObjectByKey(CurrentObject, $"{_comboBox.Value}");
            WriteValue();
        }

        protected override object GetControlValueCore(){
            return PropertyValue;
        }

        private IModelASPxClientLookupPropertyEditor EditorModel => ((IModelMemberViewItemASPxClientLookupPropertyEditor) Model).ASPxClientLookupPropertyEditor;
        public ObjectView ParentView{ get; set; }
        
        protected override string GetPropertyDisplayValue(){
            return _helper.GetEscapedDisplayText(PropertyValue,NullText,DisplayFormat);
        }

        public override void Setup(IObjectSpace space, XafApplication xafApplication){
            base.Setup(space, xafApplication);
            _helper = new WebLookupEditorHelper(application, objectSpace, MemberInfo.MemberTypeInfo, Model){
                EditorMode = LookupEditorMode.AllItems
            };
            ImmediatePostData = false;
            var lookupListViewModel = _helper.LookupListViewModel;
            _lookupViewModelMemberViewItems = lookupListViewModel.VisibleMemberViewItems().OrderForView();
            _clientStorage = ((IModelOptionsClientDatasource) Model.Application.Options).ClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
        }

        private void ConfigureClientSideEvents(ASPxComboBox comboBox){
            var clientStorage = ((IModelOptionsClientDatasource) Model.Application.Options).ClientDatasource.ClientStorage.ToString().FirstCharacterToLower();
            var displayText = _helper.GetDisplayText(PropertyValue,NullText,DisplayFormat);
            var objectKey = _helper.GetObjectKey(PropertyValue);
            comboBox.ClientSideEvents.Init = $"function(s,e){{EditorInit(s,e,'{_uniqueID}','{ParentView.Id}','{clientStorage}','{displayText}','{objectKey}');}}";
        }

        private void ConfigureColumns(ASPxComboBox comboBox){
            comboBox.Columns.Clear();
            foreach (var memberViewItem in _lookupViewModelMemberViewItems){
                var listBoxColumn = comboBox.Columns.Add(memberViewItem.ModelMember.Name);
                listBoxColumn.Caption = memberViewItem.Caption;
                var clientVisible = ((IModelColumnClientVisible) memberViewItem).ClientVisible;
                if (clientVisible.HasValue){
                    listBoxColumn.ClientVisible = clientVisible.Value;
                }
            }
        }

        private void ConfigureSynchronize(ASPxComboBox comboBox){
            var synchronizeMemberViewItem = EditorModel.SynchronizeMemberViewItem;
            if (synchronizeMemberViewItem != null){
                var typeToSynchronize = synchronizeMemberViewItem.GetLookupListView().AsObjectView.ModelClass.TypeInfo.FullName;
                var bindingName = EditorModel.SynchronizeMemberLookupColumn.ModelMember.MemberInfo.BindingName;
                var synchronizeEditor = synchronizeMemberViewItem.GetLookupListView().GetUniqueID(ParentView.Id);
                ClientSideEventsHelper.AssignClientHandlerSafe(comboBox, nameof(comboBox.ClientSideEvents.ValueChanged),
                    $"function(s,e){{SynchronizeLookupValue(s,'{_uniqueID}','{synchronizeEditor}','{bindingName}','{typeToSynchronize}')}}",
                    $"{GetType().FullName}{nameof(ConfigureSynchronize)}{nameof(comboBox.ClientSideEvents.ValueChanged)}{_uniqueID}");
            }
        }

        private void ConfigureCascade(ASPxComboBox comboBox){
            var cascadeMemberViewItem = EditorModel.CascadeMemberViewItem;
            if (cascadeMemberViewItem != null){
                var cascadeMemberName = cascadeMemberViewItem.GetLookupListView().GetUniqueID(ParentView.Id);
                ClientSideEventsHelper.AssignClientHandlerSafe(comboBox, nameof(comboBox.ClientSideEvents.ValueChanged),
                    $"function(s,e){{FilterLookup(s,'{_clientStorage}','{cascadeMemberName}','{EditorModel.CascadeColumnFilter.Caption}')}}",
                    $"{GetType().FullName}{nameof(ConfigureCascade)}{nameof(comboBox.ClientSideEvents.ValueChanged)}{_uniqueID}");
            }
        }

        private ASPxComboBox _comboBox;

        IList<string> IDependentPropertyEditor.MasterProperties => _helper.MasterProperties;

    }


}