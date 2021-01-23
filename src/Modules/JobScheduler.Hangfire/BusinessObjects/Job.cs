using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using Fasterflect;
using Hangfire;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.Extensions.XAF.ObjectExtensions;
using Xpand.Extensions.XAF.Xpo.ValueConverters;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects {
    [DefaultProperty(nameof(Id))]
    [DefaultClassOptions][NavigationItem("JobScheduler")]
    [Appearance("PauseAction",AppearanceItemType.Action, nameof(IsPaused)+"=1",Visibility = ViewItemVisibility.Hide,TargetItems = nameof(JobSchedulerService.PauseJob))]
    [Appearance("ResumeAction",AppearanceItemType.Action, nameof(IsPaused)+"=0",Visibility = ViewItemVisibility.Hide,TargetItems = nameof(JobSchedulerService.ResumeJob))]
    public class  Job:CustomBaseObject {
        public Job(Session session) : base(session) {
        }
        ObjectType _jobType;

        [RuleRequiredField]
        [DataSourceProperty(nameof(JobTypes))]
        [Size(SizeAttribute.Unlimited)]
        [ValueConverter(typeof(ObjectTypeValueConverter))]
        [Persistent]
        public ObjectType JobType {
            get => _jobType;
            set {
                SetPropertyValue(nameof(JobType), ref _jobType, value);
                OnChanged(nameof(JobMethod));
            }
        }

        [InvisibleInAllViews]
        public bool IsPaused => JobStorage.Current.GetConnection().GetAllItemsFromSet(JobSchedulerService.PausedJobsSetName).Contains(Id);

        ObjectString _jobMethod;
        [ValueConverter(typeof(ObjectStringValueConverter))]
        [Persistent]
        [DataSourceProperty(nameof(JobMethods))]
        [Size(SizeAttribute.Unlimited)][RuleRequiredField]
        public ObjectString JobMethod {
            get => _jobMethod;
            set => SetPropertyValue(nameof(JobMethod), ref _jobMethod, value);
        }

        [Browsable(false)]
        public IList<ObjectString> JobMethods 
            => AppDomain.CurrentDomain.JobMethods().Where(info => info.DeclaringType==JobType?.Type)
                .Select(info => new ObjectString(info.Attribute<JobProviderAttribute>()?.DisplayName??info.Name.CompoundName() ))
                .ToArray();
        
        [Browsable(false)]
        public IList<ObjectType> JobTypes => AppDomain.CurrentDomain.JobMethods().Select(m=>m.DeclaringType).Distinct()
            .Select(type => new ObjectType(type){Name = type.Attribute<JobProviderAttribute>().DisplayName??type.Name.CompoundName()})
            .ToArray();

        public override void AfterConstruction() {
            base.AfterConstruction();
            _cronExpression = Session.GetObjectByKey<CronExpression>(nameof(Cron.Daily));
        }

        CronExpression _cronExpression;
        [RuleRequiredField]
        public CronExpression CronExpression {
            get => _cronExpression;
            set => SetPropertyValue(nameof(CronExpression), ref _cronExpression, value);
        }

        [Association("Job-Jobs")][CollectionOperationSet(AllowAdd = false,AllowRemove = false)]
        public XPCollection<JobWorker> Workers => GetCollection<JobWorker>(nameof(Workers));

        string _id;
        [RuleUniqueValue][RuleRequiredField][VisibleInListView(true)]
        public string Id {
            get => _id;
            set => SetPropertyValue(nameof(Id), ref _id, value);
        }
    }

    public enum WorkerState {
        Scheduled,
        Enqueued,
        Processing,
        Succeeded,
        Failed,
        Awaiting,
        Deleted
    }
}