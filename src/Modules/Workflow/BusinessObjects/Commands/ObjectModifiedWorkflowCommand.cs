using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.Workflow.BusinessObjects.Commands{
    [DefaultProperty(nameof(Description))]
    [System.ComponentModel.DisplayName("Object Modified")]
    [ImageName("ObjectModifiedWorkflowCommand")]
    [OptimisticLocking(OptimisticLockingBehavior.NoLocking)]
    public class ObjectModifiedWorkflowCommand(Session session) :WorkflowCommand(session){
        protected override Type GetReturnType() => typeof(object[]);

        string _criteria;

        [EditorAlias(DevExpress.ExpressApp.Editors.EditorAliases.CriteriaPropertyEditor)]
        [CriteriaOptions(nameof(CriteriaType))][Size(-1)]
        [ToolTip("Optional. A filter criteria to apply. The command will only trigger if the modified object matches this criteria at the time of the commit.")]
        public string Criteria{
            get => _criteria;
            set => SetPropertyValue(nameof(Criteria), ref _criteria, value);
        }

        [Browsable(false)]
        public Type CriteriaType => CaptionHelper.ApplicationModel.BOModel
            .FirstOrDefault(@class => @class.Name == Object?.Name)?.TypeInfo.Type;
        ObjectString _object;

        [DataSourceProperty(nameof(Objects))][RuleRequiredField]
        [ToolTip("The business object type to monitor for changes.")]
        public ObjectString Object{
            get => Objects.FirstOrDefault(s => s.Name==_objectName);
            set{
                if (SetPropertyValue(nameof(Object), ref _object, value)){
                    _objectName = value.Name;
                } }
        }

        ObjectString _member;

        [DataSourceProperty(nameof(Members))][RuleRequiredField]
        [ToolTip("The specific property on the object to monitor. The command will only trigger when this property is modified.")]
        public ObjectString Member{
            get => Members.FirstOrDefault(s => s.Name==_memberName);
            set{
                if (SetPropertyValue(nameof(Member), ref _member, value)){
                    _memberName = value.Name;
                } }
        }

        string _memberName;

        [Browsable(false)]
        public string MemberName{
            get => _memberName;
            set => SetPropertyValue(nameof(MemberName), ref _memberName, value);
        }

        string _objectName;

        [Browsable(false)][Size(-1)]
        public string ObjectName{
            get => _objectName;
            set => SetPropertyValue(nameof(ObjectName), ref _objectName, value);
        }

        
        [Browsable(false)]
        public List<ObjectString> Objects 
            =>ObjectSpace.TypesInfo?.PersistentTypes.Where(info => info.IsPersistent).Select(info => {
                var modelClass = info.GetModelClass();
                return modelClass == null ? null : new ObjectString{
                        Name = modelClass.Name,
                        Caption = modelClass.Caption
                    };
            }).WhereNotDefault().ToList()??new List<ObjectString>();
        
        [Browsable(false)]
        public List<ObjectString> Members 
            => ObjectSpace.TypesInfo?.FindTypeInfo(ObjectName)?.Members.Where(info => !info.IsService&&info.IsPersistent&&info.IsPublic&&!info.IsReadOnly).Select(info => new ObjectString{
                Caption = info.Owner.GetModelClass().GetMemberCaption(info.Name), Name = info.Name
            }).ToList() ?? new List<ObjectString>();

        protected override bool GetNeedSubscription() => false;

        
        public override IObservable<object[]> Execute(XafApplication application, params object[] objects){
            if (ObjectName==null) return Observable.Empty<object[]>();
            var type = application.TypesInfo.FindTypeInfo(ObjectName).Type;
            return application.WhenProviderCommitted(type,[Member.Name],ObjectModification.NewOrUpdated)
                .Select(t => (t.objectSpace,objects:t.objects.Where(o => type.IsInstanceOfType(o))
                    .Where(o => t.objectSpace.IsObjectFitForCriteria( Criteria, o))
                    .Select(o => (o,key:t.objectSpace.GetKeyValue(o))).ToArray()))
                .Where(t => t.objects.Length>0)
                .BufferUntilInactive(2.Seconds()).WhenNotEmpty()
                .Select(list => list.SelectMany(t => t.objects)
                    .GroupBy(o => $"{o.o.GetType()}{o.key}")
                    .Select(group => group.Last().o)
                    .ToArray())
                .Select(objects1 => objects1);
            
        }
    }

    
}