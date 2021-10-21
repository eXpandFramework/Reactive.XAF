using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.Reactive;
using EditorAliases = DevExpress.ExpressApp.Editors.EditorAliases;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Notification.BusinessObjects {
    [Appearance("Hide Channels",AppearanceItemType.ViewItem,nameof(RegisteredChannels)+"=0",Visibility = ViewItemVisibility.Hide,TargetItems = nameof(NotificationChannels))]
    public class ObjectStateNotification : Job {
        public ObjectStateNotification(Session session) : base(session) {
        }

        [Browsable(false)]
        public int RegisteredChannels => typeof(NotificationChannel.NotificationChannel).GetTypeInfo().Descendants.Count(info => !info.IsAbstract);
        [Association("NotificationJob-NotificationJobIndexs")][Browsable(false)][Aggregated]
        public XPCollection<NotificationJobIndex> NotificationJobIndexs 
            => GetCollection<NotificationJobIndex>(nameof(NotificationJobIndexs));
        
        [Association("NotificationChannel-NotificationJobs")]
        public XPCollection<NotificationChannel.NotificationChannel> NotificationChannels 
            => GetCollection<NotificationChannel.NotificationChannel>(nameof(NotificationChannels));
        
        
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