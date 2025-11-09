using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.XAF.Modules.Workflow.Services;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [DefaultProperty(nameof(Description))]
    [System.ComponentModel.DisplayName("Object Exists")]
    [ImageName("ObjectExists")]
    [OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    public class ObjectExistWorkflowCommand(Session session) :WorkflowCommand(session){
        protected override Type GetReturnType() => CriteriaType?.MakeArrayType()??base.GetReturnType();

        string _criteria;

        [EditorAlias(DevExpress.ExpressApp.Editors.EditorAliases.CriteriaPropertyEditor)]
        [CriteriaOptions(nameof(CriteriaType))][Size(-1)]
        [ToolTip("The filter criteria (in XAF format) to apply when querying for objects.")]
        public string Criteria{
            get => _criteria;
            set => SetPropertyValue(nameof(Criteria), ref _criteria, value);
        }

        [Browsable(false)]
        public Type CriteriaType => ObjectSpace.TypesInfo.FindTypeInfo(ObjectName)?.Type;
        
        ObjectString _object;

        [DataSourceProperty(nameof(ObjectTypes))][RuleRequiredField]
        [ToolTip("The business object type to query.")]
        public ObjectString Object{
            get => ObjectTypes.FirstOrDefault(s => s.Name==_objectName);
            set{
                if (SetPropertyValue(nameof(Object), ref _object, value)){
                    ObjectName = value.Name;
                } }
        }

        string _objectName;

        [Browsable(false)][Size(-1)]
        public string ObjectName{
            get => _objectName;
            set => SetPropertyValue(nameof(ObjectName), ref _objectName, value);
        }

        
        [Browsable(false)]
        public List<ObjectString> ObjectTypes 
            =>ObjectSpace.TypesInfo?.PersistentTypes.Select(info => {
                var objectString = new ObjectString();
                var modelClass = info.GetModelClass();
                objectString.Name = info.FullName;
                objectString.Caption = modelClass?.Caption;
                return objectString;
            }).ToList()??[];

        protected override void OnChanged(string propertyName, object oldValue, object newValue){
            base.OnChanged(propertyName, oldValue, newValue);
            if (propertyName is nameof(Object) or nameof(Criteria)){
                if (propertyName == nameof(Object)){
                    Criteria = null;
                    OutputProperty = null;
                }
                OnChanged(nameof(Objects));
            }
        }

        [ReadOnlyCollection]
        public List<ObjectString> Objects{
            get{
                try{
                    if (CriteriaType == null) return[];
                    var objects = StartAction is not ObjectExistWorkflowCommand objectExistCommand ? []
                        :objectExistCommand.ToOutputValue( objectExistCommand.GetObjects( objectExistCommand.GetCriteria( [])).ToArray());
                    var list = this.GetObjects( this.GetCriteria( objects));
                    return this.ToOutputValue(list.ToArray()).Select(o => new ObjectString($"{o}")).ToList();
                }
                catch (Exception){
                    return new();
                }
            }
        }


        public override void AfterConstruction(){
            base.AfterConstruction();
            _objectModification=ObjectModification.NewOrUpdated;
        }

        ObjectModification _objectModification;

        [Appearance("Disable for existing",nameof(SearchMode)+"='"+nameof(CommandSearchMode.Existing)+"'",Enabled = false)]
        [ToolTip("Specifies whether the command should react to new objects, updated objects, or both. Only applies when SearchMode is 'Default' or 'Commits'.")]
        public ObjectModification ObjectModification{
            get => _objectModification;
            set => SetPropertyValue(nameof(ObjectModification), ref _objectModification, value);
        }
        protected override bool GetNeedSubscription() => false;

        CommandSearchMode _searchMode;
        [ToolTip("Defines the query strategy: 'Default' finds existing objects and listens for new ones; 'Existing' only finds current objects; 'Commits' only listens for new/updated objects.")]
        public CommandSearchMode SearchMode{
            get => _searchMode;
            set => SetPropertyValue(nameof(SearchMode), ref _searchMode, value);
        }
        
        public override IObservable<object[]> Execute(XafApplication application, params object[] objects) 
            => this.InvokeObjectExistCommand(application, objects);

        string _outputProperty;
        [Size(SizeAttribute.Unlimited)]
        [ModelDefault("RowCount","1")]
        [Description("You can format the output like ID: {Oid}, Status: {Status}")][ToolTip("Optional. Formats the output. Can be a single property name (e.g., 'Oid') or a composite string (e.g., 'ID: {Oid}, Status: {Status}').")]
        public string OutputProperty{
            get => _outputProperty;
            set => SetPropertyValue(nameof(OutputProperty), ref _outputProperty, value);
        }

        string _inputFilterProperty;
        [ToolTip("Optional. The name of a property on the target object. The command will filter for objects where this property's value matches one of the values received as input from a previous command.")]
        public string InputFilterProperty{
            get => _inputFilterProperty;
            set => SetPropertyValue(nameof(InputFilterProperty), ref _inputFilterProperty, value);
        }

        int _topReturnObjects;
        [ToolTip("Limits the query to return only the specified number of objects.")]
        public int TopReturnObjects{
            get => _topReturnObjects;
            set => SetPropertyValue(nameof(TopReturnObjects), ref _topReturnObjects, value);
        }

        int _skipTopReturnObjects;
        [ToolTip("Skips the specified number of objects from the beginning of the query result. Requires at least one Sort Property to be defined.")]
        public int SkipTopReturnObjects{
            get => _skipTopReturnObjects;
            set => SetPropertyValue(nameof(SkipTopReturnObjects), ref _skipTopReturnObjects, value);
        }
        
        [Association("ObjectExistCommand-CommandSortProperties")][Aggregated][RuleRequiredField(TargetCriteria = nameof(SkipTopReturnObjects)+">0")]
        [ToolTip("Defines the sort order for the query, which is required for stable results when using 'SkipTopReturnObjects'.")]
        public XPCollection<CommandSortProperty> SortProperties => GetCollection<CommandSortProperty>();
    }

    public enum CommandSearchMode{
        Default,Existing,Commits
    }
}