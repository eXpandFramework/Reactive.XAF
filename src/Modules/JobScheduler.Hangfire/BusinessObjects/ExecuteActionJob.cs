using System.Collections.Generic;
using System.Linq;
using DevExpress.Blazor.Internal.Compatibility.System.ComponentModel;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [Appearance("DisableSelectedObjectsCriteria",AppearanceItemType.ViewItem, nameof(Object)+" Is Null",TargetItems = nameof(SelectedObjectsCriteria),Enabled = false)]
    public class ExecuteActionJob : Job {
        public ExecuteActionJob(Session session) : base(session) {
        }

        public override void AfterConstruction() {
            base.AfterConstruction();
            JobType = new ObjectType(typeof(Jobs.ExecuteActionJob));
            JobMethod = new ObjectString(nameof(Jobs.ExecuteActionJob.Execute));
        }

        ObjectString _action;
        [DataSourceProperty(nameof(Actions))]
        [RuleRequiredField]
        [ValueConverter(typeof(ObjectStringValueConverter))]
        [Persistent]
        public ObjectString Action {
            get => _action;
            set => SetPropertyValue(nameof(Action), ref _action, value);
        }

        [Browsable(false)]
        public IList<ObjectString> Actions 
            => CaptionHelper.ApplicationModel.ActionDesign.Actions
                .Select(action => action.GetValue<ActionBase>(ModelActionsNodesGenerator.ActionPropertyName)).OfType<SimpleAction>()
                .Select(action => new ObjectString(action.Id) {Caption = action.Caption}).ToArray();

        [Browsable(false)]
        public IList<ObjectString> Views 
            => CaptionHelper.ApplicationModel.Views.Where(view => Object==null|| (view is IModelObjectView modelObjectView&& modelObjectView.ModelClass.TypeInfo.Type==Object.Type))
                .Select(modelView => new ObjectString(modelView.Id)).ToArray();

        string _selectedObjectsCriteria;
        ObjectString _view;
        [RuleRequiredField][DataSourceProperty(nameof(Views))]
        [ValueConverter(typeof(ObjectStringValueConverter))]
        [Persistent]
        public ObjectString View {
            get => _view;
            set => SetPropertyValue(nameof(View), ref _view, value);
        }

        ObjectType _object;

        [DataSourceProperty(nameof(Objects))]
        [ValueConverter(typeof(ObjectTypeValueConverter))]
        [Persistent]
        public ObjectType Object {
            get => _object;
            set {
                if (SetPropertyValue(nameof(Object), ref _object, value)) {
                    OnChanged(nameof(View));
                }
            }
        }

        [Browsable(false)]
        public IList<ObjectType> Objects 
            => CaptionHelper.ApplicationModel.BOModel
                .Select(modelClass => new ObjectType(modelClass.TypeInfo.Type) {Name = modelClass.Caption}).ToArray();

        [CriteriaOptions(nameof(Object)+"."+nameof(ObjectType.Type))]
        [EditorAlias(EditorAliases.CriteriaPropertyEditor), Size(SizeAttribute.Unlimited)]
        public string SelectedObjectsCriteria {
            get => _selectedObjectsCriteria;
            set => SetPropertyValue(nameof(SelectedObjectsCriteria), ref _selectedObjectsCriteria, value);
        }
    }
}