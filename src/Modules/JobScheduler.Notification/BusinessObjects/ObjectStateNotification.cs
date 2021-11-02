using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using EditorAliases = DevExpress.ExpressApp.Editors.EditorAliases;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects {
    
    public class ObjectStateNotification : Job {
        public ObjectStateNotification(Session session) : base(session) {
        }

        [Browsable(false)]
        public override bool UseChainJob => true;

        [Association("NotificationJob-NotificationJobIndexs")][Browsable(false)][Aggregated]
        public XPCollection<NotificationJobIndex> NotificationJobIndexs 
            => GetCollection<NotificationJobIndex>(nameof(NotificationJobIndexs));

        string _selectedObjectsCriteria;
        
        public override void AfterConstruction() {
            base.AfterConstruction();
            JobType = new ObjectType(typeof(Jobs.NotificationJob));
            JobMethod = new ObjectString(nameof(Jobs.NotificationJob.Execute));
        }
        ObjectType _object;

        [DataSourceProperty(nameof(Objects))]
        [ValueConverter(typeof(ObjectTypeValueConverter))]
        [Persistent][RuleRequiredField]
        public ObjectType Object {
            get => _object;
            set => SetPropertyValue(nameof(Object), ref _object, value);
        }

        [Browsable(false)]
        public IList<ObjectType> Objects 
            => ((IModelJobSchedulerNotification)CaptionHelper.ApplicationModel
                    .ToReactiveModule<IModelReactiveModulesJobScheduler>().JobScheduler).Notification.Types
                .Select(type => new ObjectType(type.Type.TypeInfo.Type) {Name = type.Type.Caption}).ToArray();

        [CriteriaOptions(nameof(Object)+"."+nameof(ObjectType.Type))]
        [EditorAlias(EditorAliases.CriteriaPropertyEditor), Size(SizeAttribute.Unlimited)]
        public string SelectedObjectsCriteria {
            get => _selectedObjectsCriteria;
            set => SetPropertyValue(nameof(SelectedObjectsCriteria), ref _selectedObjectsCriteria, value);
        }
    }
}